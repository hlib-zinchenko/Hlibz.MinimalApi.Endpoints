using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Envelope.Api.Books;

/// <summary>
/// Lists every book in the catalog.
/// </summary>
internal sealed class ListBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet(string.Empty, Handle);

    private static Result<IReadOnlyList<Book>> Handle(BookStore store) =>
        Result<IReadOnlyList<Book>>.Success(store.All);
}
