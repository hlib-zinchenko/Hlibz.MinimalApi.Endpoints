using Hlibz.MinimalApi.Endpoints;

namespace Library.Api.Books;

internal sealed class GetBookEndpoint : IEndpoint<BooksEndpointGroup, Book>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id:int}";

    public IReadOnlyList<int> ProducesErrors => [StatusCodes.Status404NotFound];

    public Delegate Handler => Handle;

    private static IResult Handle(int id, BookStore store) =>
        store.Find(id) is { } book ? TypedResults.Ok(book) : TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: $"Book {id} was not found.");
}
