using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders;

internal sealed record OrderDto(int Id);

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

    public Delegate Handler => Handle;

    private static IResult Handle(int id) =>
        id > 0 ? Results.Ok(new OrderDto(id)) : Results.NotFound();
}

internal sealed class CreateOrderEndpoint : IEndpoint<OrdersEndpointGroup, OrderDto>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => string.Empty;

    public int SuccessStatusCode => StatusCodes.Status201Created;

    public Delegate Handler => () => Results.Created("api/orders/1", new OrderDto(1));
}

internal sealed class ExportOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "export";

    public string? FileContentType => "text/csv";

    public Delegate Handler => () => Results.Text("id\n1\n", "text/csv");
}

internal sealed class ImportOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => "import";

    public long? MaxUploadBytes => 1024;

    public Delegate Handler => (IFormFile file) => Results.Ok(file.Length);
}

internal sealed class MyOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "mine";

    public string? AuthorizationPolicy => "Member";

    public IReadOnlyList<int> ProducesErrors => [StatusCodes.Status401Unauthorized];

    public Delegate Handler => () => Results.Ok();
}

internal sealed class SearchOrdersEndpoint : IEndpoint<OrdersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "search";

    public string? RateLimitPolicy => "Search";

    public Delegate Handler => () => Results.Ok();
}

internal sealed class CountOrdersEndpoint : IEndpoint<OrdersEndpointGroup, int>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "count";

    public Delegate Handler => () => Results.Ok(1);

    public void Configure(RouteHandlerBuilder builder)
    {
        builder.WithSummary("Counts the orders.");
    }
}
