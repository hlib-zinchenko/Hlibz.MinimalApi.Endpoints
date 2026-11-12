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
/// Adds a book to the catalog. Members only.
/// </summary>
/// <remarks>
/// The request is validated before the handler runs (<c>AddFluentValidation</c>).
/// </remarks>
internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, Handle)
            .RequireAuthorization(Policies.Member);

    private static Created<Book> Handle([FromBody] AddBookRequest request, BookStore store)
    {
        Book book = store.Add(request.Title, request.Author);
        return TypedResults.Created($"/api/books/{book.Id}", book);
    }
}
