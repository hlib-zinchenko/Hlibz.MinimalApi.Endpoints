using Hlibz.MinimalApi.Endpoints;

using Library.Api.Books;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Library.Api.Admin;

/// <summary>
/// Deletes a book from the catalog. Admins only.
/// </summary>
/// <remarks>
/// Admin-only through the admin group convention; 401/403 are documented because the endpoint
/// ends up requiring authorization. The handler's return type documents 204;
/// <c>ProducesErrors</c> gives its body-less 404 the error schema.
/// </remarks>
internal sealed class DeleteBookEndpoint : IEndpoint<AdminBooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapDelete("{id:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Results<NoContent, NotFound> Handle(int id, BookStore store) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
}
