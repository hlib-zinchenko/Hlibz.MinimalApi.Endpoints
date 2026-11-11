![Hlibz.MinimalApi.Endpoints icon](https://raw.githubusercontent.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/main/docs/icon.png)

# Hlibz.MinimalApi.Endpoints

[![NuGet](https://img.shields.io/nuget/v/Hlibz.MinimalApi.Endpoints.svg)](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints)
[![CI](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml/badge.svg)](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints/actions/workflows/ci.yml)

One class per minimal API endpoint, found and mapped for you, with its OpenAPI responses
documented from what the endpoint is.

```bash
dotnet add package Hlibz.MinimalApi.Endpoints
```

```csharp
internal sealed class OrdersEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/orders";

    public IReadOnlyList<string> Tags => ["Orders"];
}

internal sealed class GetOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id:int}";

    public IReadOnlyList<int> ProducesErrors => [StatusCodes.Status404NotFound];

    public string? AuthorizationPolicy => "Member";

    public Delegate Handler => (int id, IOrderService orders) => orders.Find(id) is { } order
        ? Results.Ok(order)
        : Results.NotFound();
}

app.MapEndpoints(typeof(Program).Assembly);
```

The endpoint is mapped into its group, named `GetOrder`, and documents `200` as `OrderDto`
plus `404`, and `401`/`403` because it ends up requiring authorization. `429` is documented
whenever it ends up rate limited, `413` for uploads (`MaxUploadBytes`), and file downloads
via `FileContentType`. Errors use `ProblemDetails` unless you set your own
(`options.UseErrorResponse<TError>()`); app-wide rules go in group and endpoint conventions
(`options.ConfigureGroups<IAdminEndpointGroup>(...)`).

Request validation: [Hlibz.MinimalApi.Endpoints.FluentValidation](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation).

Full documentation and a sample: [https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints).
