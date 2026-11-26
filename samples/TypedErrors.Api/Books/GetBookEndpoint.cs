using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace TypedErrors.Api.Books;

/// <summary>
/// Gets one book by its id.
/// </summary>
/// <remarks>
/// The 404 needs no ProducesErrors: GetBookError says the handler can fail with BookNotFound.
/// </remarks>
internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) => group.MapGet("{id:int}", Handle);

    private static Result<Book, GetBookError> Handle(int id, BookStore store) =>
        store.Find(id) is { } book ? book : new BookNotFound(id);
}
