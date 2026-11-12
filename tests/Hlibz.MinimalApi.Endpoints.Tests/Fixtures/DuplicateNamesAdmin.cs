using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.DuplicateNames.Admin;

internal sealed class AdminBooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/admin/books";
}

internal sealed class GetBookEndpoint : IEndpoint<AdminBooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id}", (string id) => Results.Ok(id));
}
