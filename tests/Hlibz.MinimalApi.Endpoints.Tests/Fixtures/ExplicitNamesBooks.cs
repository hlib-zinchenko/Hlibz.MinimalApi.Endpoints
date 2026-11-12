using Microsoft.AspNetCore.Http;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.ExplicitNames.Books;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";
}

internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id}";

    public Delegate Handler => (string id) => Results.Ok(id);
}
