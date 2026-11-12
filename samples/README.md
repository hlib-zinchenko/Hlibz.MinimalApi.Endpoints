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
