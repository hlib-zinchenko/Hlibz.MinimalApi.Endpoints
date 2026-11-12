using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.DuplicateNames.Admin;

internal sealed class AdminBooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/admin/books";
}

internal sealed class GetBookEndpoint : IEndpoint<AdminBooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id}";

    public Delegate Handler => (string id) => Results.Ok(id);
}
