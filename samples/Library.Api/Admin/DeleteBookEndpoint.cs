using Hlibz.MinimalApi.Endpoints;

using Library.Api.Books;

namespace Library.Api.Admin;

/// <summary>
/// Deletes a book. Admins only, through the admin group convention; 401/403 are documented
/// because the endpoint ends up requiring authorization.
/// </summary>
internal sealed class DeleteBookEndpoint : IEndpoint<AdminBooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Delete;

    public string Route => "{id:int}";

    public IReadOnlyList<int> ProducesErrors => [StatusCodes.Status404NotFound];

    public Delegate Handler => Handle;

    private static IResult Handle(int id, BookStore store) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
}
