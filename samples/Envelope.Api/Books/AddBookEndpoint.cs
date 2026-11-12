using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;

using Microsoft.AspNetCore.Mvc;

namespace Envelope.Api.Books;

public sealed record AddBookRequest(string Title, string Author);

/// <summary>
/// Adds a book to the catalog. Members only.
/// </summary>
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .RequireAuthorization("member")
            .ProducesErrors(StatusCodes.Status400BadRequest);

    private static Result<Book> Handle([FromBody] AddBookRequest request, BookStore store)
    {
        Dictionary<string, string[]> errors = [];
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(request.Title)] = ["Enter a title."];
        }

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            errors[nameof(request.Author)] = ["Enter an author."];
        }

        return errors.Count > 0
            ? Result<Book>.Invalid(errors)
            : Result<Book>.Success(store.Add(request.Title, request.Author));
    }
}
