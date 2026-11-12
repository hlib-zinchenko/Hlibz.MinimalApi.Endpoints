using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders;

internal sealed record OrderDto(int Id);

internal sealed class OrdersEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/orders";

    public IReadOnlyList<string> Tags => ["Orders"];
}

/// <summary>
/// A typed union whose body-less <c>404</c> the declared error replaces.
/// </summary>
internal sealed class GetOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Results<Ok<OrderDto>, NotFound> Handle(int id) =>
        id > 0 ? TypedResults.Ok(new OrderDto(id)) : TypedResults.NotFound();
}

internal sealed class CreateOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle);

    private static Created<OrderDto> Handle() =>
        TypedResults.Created("api/orders/1", new OrderDto(1));
}

/// <summary>
/// A <c>409</c> the handler documents with a body of its own, which the declared error leaves.
/// </summary>
internal sealed class ReserveOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("{id:int}/reserve", Handle)
            .ProducesErrors(StatusCodes.Status409Conflict);

    private static Results<Ok<OrderDto>, Conflict<OrderDto>> Handle(int id) =>
        id > 0 ? TypedResults.Ok(new OrderDto(id)) : TypedResults.Conflict(new OrderDto(id));
}

/// <summary>
/// An opaque handler under <c>TResponse</c>: the success response comes from the fallback.
/// </summary>
internal sealed class LegacyOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
#pragma warning disable HMAE009 // Opaque on purpose: the fallback documents 200 OrderDto.
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("legacy", () => Results.Ok(new OrderDto(1)));
#pragma warning restore HMAE009
}

internal sealed class ExportOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("export", () => Results.Text("id\n1\n", "text/csv"))
            .ProducesFile("text/csv");
}

internal sealed class ImportOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("import", (IFormFile file) => Results.Ok(file.Length))
            .AcceptsUpload(1024);
}

internal sealed class MyOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("mine", () => Results.Ok())
            .RequireAuthorization("Member")
            .ProducesErrors(StatusCodes.Status401Unauthorized);
}

internal sealed class SearchOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("search", () => Results.Ok())
            .RequireRateLimiting("Search");
}

internal sealed class CountOrdersEndpoint : IEndpoint<OrdersEndpointGroup, int>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("count", () => TypedResults.Ok(1))
            .WithSummary("Counts the orders.");
}
