using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.NullMap;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";
}

internal sealed class BrokenEndpoint : IEndpoint<BooksEndpointGroup>
{
#pragma warning disable HMAE007 // Maps nothing on purpose: MapEndpoints must reject the null.
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) => null!;
#pragma warning restore HMAE007
}
