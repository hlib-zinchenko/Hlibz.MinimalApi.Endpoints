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

## TypedErrors.Api

A book catalog whose handlers declare their errors with
[Hlibz.Results.AspNetCore](https://www.nuget.org/packages/Hlibz.Results.AspNetCore) 3.0: each
returns `Result<T, TError>`, `TError` a C# `union` of error types (`Books/BookErrors.cs`,
`Books/BookErrorSets.cs`). `WithResultResponses()`, applied to every endpoint with one
`ConfigureEndpoints` call, sends the result and documents each declared error's status from the
error set; `UseErrorResponse<ErrorResponse>()` gives what this package documents the same body.

| Endpoint | Documented | From |
|---|---|---|
| `GET api/books/{id}` | `200 Book`, `404` | `Result<Book, GetBookError>`: `BookNotFound`, its id in `details`. No `ProducesErrors`. |
| `POST api/books` | `201 Book` + `Location`, `409`, `400`, `401`/`403` | `AddBookError`'s `BookExists` (the existing book's id); `.ProducesErrors(400)` for the ambient `ValidationFailed` the handler returns; the member policy; `.WithCreatedLocation<Book>(...)`. |
| `DELETE api/books/{id}` | `200`, `404`, `409`, `401`/`403` | `Result<Unit, DeleteBookError>`: `BookNotFound`, or `BookOnLoan` with who has the book. |

```csharp
internal sealed class DeleteBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{id:int}", Handle).RequireAuthorization("member");

    private static Result<Unit, DeleteBookError> Handle(int id, BookStore store)
    {
        if (store.Find(id) is null)
        {
            return new BookNotFound(id);
        }

        IReadOnlyList<string> borrowers = store.BorrowersOf(id);
        if (borrowers.Count > 0)
        {
            return new BookOnLoan(borrowers);
        }

        store.Remove(id);
        return Unit.Value;
    }
}
```

```bash
dotnet run --project samples/TypedErrors.Api    # http://localhost:5191
```

`TypedErrors.Api.http` has every case, with the same `X-User`/`X-Role` sign-in as `Library.Api`;
the OpenAPI document is at `/openapi/v1.json`. Every error is Hlibz.Results' `ErrorResponse` there;
[Hlibz.OpenApi.Results](https://www.nuget.org/packages/Hlibz.OpenApi.Results) would give each
error its own body, which Hlibz.Stack shows.
