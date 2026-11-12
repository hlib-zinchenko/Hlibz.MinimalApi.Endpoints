# Samples

`Library.Api`: a small book catalog.

| Endpoint | What it shows |
|---|---|
| `GET api/books` | `TResponse` checked against `Ok<Book[]>`, `.RequireRateLimiting(...)` (`429` documented), an `[AsParameters]` query validated by FluentValidation. |
| `GET api/books/{id}` | `Results<Ok<Book>, ProblemHttpResult>` plus `.ProducesErrors(404)` (`404` as `ProblemDetails`). |
| `POST api/books` | A `Created<Book>` handler (`201` documented by its return type), `.RequireAuthorization(...)` (`401`/`403`), a validated body. |
| `GET api/books/export` | `.ProducesFile("text/csv")`: a CSV download. |
| `POST api/books/import` | `.AcceptsUpload(64 * 1024)`: a 64 KB upload (`413` enforced and documented). |
| `DELETE api/admin/books/{id}` | An admin group gated by a group convention, not by the endpoint; `Results<NoContent, NotFound>` documents `204`, and `.ProducesErrors(404)` gives the `404` the error schema. |

Each endpoint's `Map` is a plain minimal API chain:

```csharp
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup, Book>
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
