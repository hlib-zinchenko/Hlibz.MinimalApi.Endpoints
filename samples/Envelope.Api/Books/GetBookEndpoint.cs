using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Envelope.Api.Books;

/// <summary>
/// Gets one book by its id.
/// </summary>
internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result<Book> Handle(int id, BookStore store) =>
        store.Find(id) is { } book
            ? Result<Book>.Success(book)
            : Result<Book>.NotFound($"Book {id} was not found.");
}
