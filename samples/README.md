# Samples

`Library.Api`: a small book catalog.

| Endpoint | What it shows |
|---|---|
| `GET api/books` | `TResponse`, a rate limit (`429` documented), an `[AsParameters]` query validated by FluentValidation. |
| `GET api/books/{id}` | `ProducesErrors` (`404` as `ProblemDetails`). |
| `POST api/books` | `SuccessStatusCode` 201, `AuthorizationPolicy` (`401`/`403`), a validated body. |
| `GET api/books/export` | `FileContentType`: a CSV download. |
| `POST api/books/import` | `MaxUploadBytes`: a 64 KB upload (`413` enforced and documented). |
| `DELETE api/admin/books/{id}` | An admin group gated by a group convention, not by the endpoint. |

```bash
dotnet run --project samples/Library.Api    # http://localhost:5190
```

`Library.Api.http` has every case. Sign in with `X-User: alice` + `X-Role: member` (or
`X-Role: admin`); the OpenAPI document is at `/openapi/v1.json`.
