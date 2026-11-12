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

    public void Configure(RouteGroupBuilder group) => group.WithTags("Orders");
}

internal sealed class GetOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .RequireAuthorization("Member")
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Results<Ok<OrderDto>, NotFound> Handle(int id, IOrderService orders) =>
        orders.Find(id) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound();
}

app.MapEndpoints(typeof(Program).Assembly);
```

`Map` is a plain minimal API chain. The endpoint is mapped into its group, named `GetOrder`,
and documents `200` as `OrderDto` (from the handler's return type) plus `404`, and
`401`/`403` because it ends up requiring authorization. `429` is documented whenever it ends
up rate limited, `413` for uploads (`.AcceptsUpload(maxBytes)`), and file downloads via
`.ProducesFile(contentType)`. The endpoint class's XML `<summary>` becomes the operation
description, read by the generator at compile time (no documentation file needed). Errors use `ProblemDetails` unless you set your own
(`options.UseErrorResponse<TError>()`); app-wide rules go in group and endpoint conventions
(`options.ConfigureGroups<IAdminEndpointGroup>(...)`).

Endpoints are listed and checked at compile time by a source generator and analyzer that ship
inside the package: no assembly scan at startup, and an endpoint that can't be mapped (no
group, no parameterless constructor, a `Map` that doesn't map exactly one route, a handler
that doesn't return `TResponse`, ...) is a build error rather than a startup failure.

Request validation: [Hlibz.MinimalApi.Endpoints.FluentValidation](https://www.nuget.org/packages/Hlibz.MinimalApi.Endpoints.FluentValidation).

Full documentation and a sample: [https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints](https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints).
