using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace TypedErrors.Api.Books;

/// <summary>
/// Removes a book from the catalog. Members only; a book on loan stays.
/// </summary>
/// <remarks>
/// Two errors on two statuses (404, 409), and BookOnLoan's 409 carries who has the book.
/// </remarks>
internal sealed class DeleteBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{id:int}", Handle).RequireAuthorization("member");

    private static Result<Unit, DeleteBookError> Handle(int id, BookStore store)
    {
        if (store.Find(id) is null)
        {
            return new BookNotFound(id);
        }

        IReadOnlyList<string> borrowers = store.BorrowersOf(id);
        if (borrowers.Count > 0)
        {
            return new BookOnLoan(borrowers);
        }

        store.Remove(id);
        return Unit.Value;
    }
}
