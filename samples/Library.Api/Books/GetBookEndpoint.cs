using Hlibz.MinimalApi.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Library.Api.Books;

/// <summary>
/// Gets one book by its id; a missing book is a <c>404</c> problem.
/// </summary>
/// <remarks>
/// The <c>404</c> is documented by <c>ProducesErrors</c> because <c>ProblemHttpResult</c>
/// documents nothing itself.
/// </remarks>
internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet("{id:int}", Handle)
            .ProducesErrors(StatusCodes.Status404NotFound);

    private static Results<Ok<Book>, ProblemHttpResult> Handle(int id, BookStore store) =>
        store.Find(id) is { } book
            ? TypedResults.Ok(book)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: $"Book {id} was not found.");
}
