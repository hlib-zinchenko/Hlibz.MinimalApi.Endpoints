using Hlibz.MinimalApi.Endpoints;

namespace Library.Api.Books;

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";

    public IReadOnlyList<string> Tags => ["Books"];
}
