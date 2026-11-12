using Hlibz.MinimalApi.Endpoints;

namespace Envelope.Api.Books;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";

    public void Configure(RouteGroupBuilder group) => group.WithTags("Books");
}
