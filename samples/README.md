# Samples

## Library.Api

A small book catalog.

| Endpoint | What it shows |
|---|---|
| `GET api/books` | `Ok<Book[]>` (`200` documented by its return type), `.RequireRateLimiting(...)` (`429` documented), an `[AsParameters]` query validated by FluentValidation. |
| `GET api/books/{id}` | `Results<Ok<Book>, ProblemHttpResult>` plus `.ProducesErrors(404)` (`404` as `ProblemDetails`). |
| `POST api/books` | A `Created<Book>` handler (`201` documented by its return type), `.RequireAuthorization(...)` (`401`/`403`), a validated body. |
| `GET api/books/export` | `.ProducesFile("text/csv")`: a CSV download. |
| `POST api/books/import` | `.AcceptsUpload(64 * 1024)`: a 64 KB upload (`413` enforced and documented). |
| `DELETE api/admin/books/{id}` | An admin group gated by a group convention, not by the endpoint; `Results<NoContent, NotFound>` documents `204`, and `.ProducesErrors(404)` gives the `404` the error schema. |

Each endpoint's `Map` is a plain minimal API chain:

```csharp
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .RequireAuthorization(Policies.Member);

    private static Created<Book> Handle([FromBody] AddBookRequest request, BookStore store) { ... }
}
```

```bash
dotnet run --project samples/Library.Api    # http://localhost:5190
```

`Library.Api.http` has every case. Sign in with `X-User: alice` + `X-Role: member` (or
`X-Role: admin`); the OpenAPI document is at `/openapi/v1.json`, rendered by ReDoc
([Hlibz.Redoc.AspNetCore](https://www.nuget.org/packages/Hlibz.Redoc.AspNetCore)) at
`/api-docs`. Each operation's description is its endpoint class's `<summary>`; notes for
maintainers are in `<remarks>`, which stay out of it.

## Envelope.Api

An API whose every body is an envelope (`{ "data", "isSuccess", "status", "message", ... }`,
from [Hlibz.Results.AspNetCore](https://www.nuget.org/packages/Hlibz.Results.AspNetCore)),
documented as one. Handlers return a `Result` or `Result<T>` and nothing else:

* **Success and failures from handlers:** Hlibz.Results' `WithResultEnvelopes()`, applied to
  every endpoint with one `ConfigureEndpoints` call in `Program.cs`. The returned result is
  sent as `ToEnvelopeHttpResult()` would send it (real status code, the app's options and JSON
  settings), and the documented `200 Result<T>` becomes `200 ResultEnvelope<T>`. Endpoints
  whose handler returns anything else are left alone.
* **Errors:** `options.UseErrorResponse<ResultEnvelope>()` in `MapEndpoints` makes every
  `ProducesErrors` status, and the `401`/`403` of endpoints requiring authorization, use the
  envelope's schema.
* **Errors outside handlers:** `AddResultErrorResponses(o => o.UseEnvelope = true)` envelopes
  the authorization middleware's `401`/`403`, and an exception handler envelopes requests
  ASP.NET can't bind (malformed JSON).

```csharp
internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result<Book> Handle(int id, BookStore store) =>
        store.Find(id) is { } book
            ? Result<Book>.Success(book)
            : Result<Book>.NotFound($"Book {id} was not found.");
}
```

```bash
dotnet run --project samples/Envelope.Api    # http://localhost:5191
```

`Envelope.Api.http` has every case, with the same `X-User`/`X-Role` sign-in as `Library.Api`.

## Todo.Api

Where three packages meet, on a small to-do list API:

* **[Hlibz.MinimalApi.Endpoints](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)**
  maps one class per endpoint, and each class's `<summary>` becomes its operation description.
* **[Hlibz.Results.AspNetCore](https://www.nuget.org/packages/Hlibz.Results.AspNetCore)** turns
  the `Result` or `Result<T>` each handler returns into its response. `WithResultResponses()`
  is applied to every endpoint with one `ConfigureEndpoints` call. It also picks the success
  status code, which follows the HTTP method unless the endpoint says otherwise. Failures keep
  their own status (`400`, `404`, `409`) and Hlibz.Results' `ErrorResponse` body, which
  `UseErrorResponse<ErrorResponse>()` documents for every `ProducesErrors` status.
* **[Hlibz.OpenApi.AspNetCore](https://www.nuget.org/packages/Hlibz.OpenApi.AspNetCore)**
  finishes the OpenAPI document the other two describe.

| Endpoint | Success | Why |
|---|---|---|
| `GET api/lists/{listId}` | `200` | The default. |
| `POST api/lists` | `201` + `Location` | `.WithCreatedLocation<TodoList>(list => $"/api/lists/{list.Id}")` in `Map`. |
| `POST api/lists/{listId}/items` | `201` + `Location` | `.WithCreatedLocation<TodoItem>(...)`, the location made from the new item. |
| `POST api/lists/{listId}/items/{itemId}/complete` | `200` | A POST keeps the default. |
| `DELETE api/lists/{listId}` | `204` | The app-wide default for DELETE. |
| `DELETE api/lists/{listId}/items/{itemId}` | `204` | The same default. |

| In the OpenAPI document | Hlibz.OpenApi.AspNetCore | Code |
|---|---|---|
| `id`, `listId` and the route parameters are integers; there's no `TodoListId` or `TodoItemId` component. | `AddStronglyTypedIds()` | `Lists/TodoListId.cs`, `Lists/TodoItemId.cs` |
| `CreateListRequest.title` and `AddItemRequest.text` carry `minLength: 1`, `pattern: \S` and "Must not be blank…" (`x-rules`). The store enforces the same rule with `Result.Invalid`, so a blank one is the documented `400`. | `AddOpenApiRules()` | `Rules/NotBlankAttribute.cs`, `Lists/Models.cs` |
| Every schema and property description comes from a `<summary>` (a record's `<param>`s too). `ErrorResponse`'s come from Hlibz.Results' own XML file, which `CopyDocumentationFilesFromPackages` puts next to the app. | `AddXmlDocumentation()` | `Lists/Models.cs`, `Todo.Api.csproj` |
| Descriptions read "The list's id.", though the summaries say "Gets the list's id.". | `AddDescriptionCleanup()` | `Program.cs` |

```csharp
// Program.cs: every DELETE that succeeds is 204; every other method stays 200.
builder.Services.AddResultErrorResponses(options =>
    options.SuccessStatusCodes[HttpMethods.Delete] = StatusCodes.Status204NoContent);

builder.Services.AddOpenApi(options => options
    .AddXmlDocumentation()
    .AddStronglyTypedIds()
    .AddOpenApiRules()
    .AddDescriptionCleanup());

app.MapEndpoints(typeof(Program).Assembly, options => options
    .UseErrorResponse<ErrorResponse>()
    .ConfigureEndpoints(endpoint => endpoint.Builder.WithResultResponses()));

/// <summary>
/// Creates a to-do list. Answers <c>201 Created</c> with the new list and its URL in
/// <c>Location</c>.
/// </summary>
internal sealed class CreateListEndpoint : IEndpoint<TodoListsEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .WithCreatedLocation<TodoList>(list => $"/api/lists/{list.Id}")
            .ProducesErrors(StatusCodes.Status400BadRequest);

    private static Result<TodoList> Handle([FromBody] CreateListRequest request, TodoStore store) =>
        store.CreateList(request.Title);
}
```

```bash
dotnet run --project samples/Todo.Api    # http://localhost:5192
```

`Todo.Api.http` has every case; no sign-in. The OpenAPI document is at `/openapi/v1.json`, and
ReDoc renders it at `/api-docs`
([Hlibz.Redoc.AspNetCore](https://www.nuget.org/packages/Hlibz.Redoc.AspNetCore)) with its
**Try it** console on (`docs.EnableTryIt()`): every operation can be sent from the page, the
`201`'s `Location` and the `204`'s empty body included.
