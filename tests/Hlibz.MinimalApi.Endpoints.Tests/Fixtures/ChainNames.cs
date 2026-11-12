using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.ChainNames;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";
}

internal sealed class ListBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet(string.Empty, () => Results.Ok())
            .WithName("FromChain");
}
