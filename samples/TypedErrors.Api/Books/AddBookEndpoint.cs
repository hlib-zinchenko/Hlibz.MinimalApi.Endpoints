using Hlibz.MinimalApi.Endpoints;
using Hlibz.Results;
using Hlibz.Results.AspNetCore;

using Microsoft.AspNetCore.Mvc;

namespace TypedErrors.Api.Books;

public sealed record AddBookRequest(string Title, string Author);

/// <summary>
/// Adds a book to the catalog. Members only.
/// </summary>
/// <remarks>
/// The 409 comes from AddBookError, the 401/403 from the member policy. ValidationFailed is an
/// ambient error, which no error set declares, so ProducesErrors documents its 400.
/// </remarks>
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .RequireAuthorization("member")
            .WithCreatedLocation<Book>(book => $"/api/books/{book.Id}")
            .ProducesErrors(StatusCodes.Status400BadRequest);

    private static Result<Book, AddBookError> Handle([FromBody] AddBookRequest request, BookStore store)
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

        if (errors.Count > 0)
        {
            return new ValidationFailed(errors);
        }

        return store.Find(request.Title, request.Author) is { } existing
            ? new BookExists(existing.Id)
            : store.Add(request.Title.Trim(), request.Author.Trim());
    }
}
