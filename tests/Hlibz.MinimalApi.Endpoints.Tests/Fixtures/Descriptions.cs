using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Descriptions;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";
}

/// <summary>
/// Lists the books, <c>20</c> per page.
/// <para>Newest first.</para>
/// </summary>
/// <remarks>Implementation notes stay out of the description.</remarks>
internal sealed class ListBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet(string.Empty, () => Results.Ok());
}

/// <summary>
/// Counts the books.
/// </summary>
internal sealed class CountBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("count", () => Results.Ok())
            .WithDescription("From Map.");
}

internal sealed class PingBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("ping", () => Results.Ok());
}
