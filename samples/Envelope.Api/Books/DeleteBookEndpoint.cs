using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

namespace Envelope.Api.Books;

/// <summary>
/// Removes a book from the catalog. Members only.
/// </summary>
internal sealed class DeleteBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{id:int}", Handle)
            .RequireAuthorization("member")
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Result Handle(int id, BookStore store) =>
        store.Remove(id) ? Result.Success() : Result.NotFound($"Book {id} was not found.");
}
