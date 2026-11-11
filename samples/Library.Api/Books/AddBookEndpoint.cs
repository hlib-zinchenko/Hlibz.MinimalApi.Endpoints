using FluentValidation;

using Hlibz.MinimalApi.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Books;

public sealed record AddBookRequest(string Title, string Author);

internal sealed class AddBookRequestValidator : AbstractValidator<AddBookRequest>
{
    public AddBookRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Author).NotEmpty().MaximumLength(50);
    }
}

/// <summary>
/// Adds a book. Members only; the request is validated before the handler runs.
/// </summary>
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup, Book>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => string.Empty;

    public int SuccessStatusCode => StatusCodes.Status201Created;

    public string? AuthorizationPolicy => Policies.Member;

    public Delegate Handler => Handle;

    private static Created<Book> Handle([FromBody] AddBookRequest request, BookStore store)
    {
        Book book = store.Add(request.Title, request.Author);
        return TypedResults.Created($"/api/books/{book.Id}", book);
    }
}
