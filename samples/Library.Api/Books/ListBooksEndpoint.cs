using FluentValidation;

using Hlibz.MinimalApi.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Library.Api.Books;

public sealed record ListBooksQuery(string? Author);

internal sealed class ListBooksQueryValidator : AbstractValidator<ListBooksQuery>
{
    public ListBooksQueryValidator()
    {
        RuleFor(query => query.Author).MaximumLength(50);
    }
}

/// <summary>
/// Lists the catalog, optionally by one author. Rate limited; a long author is a 400.
/// </summary>
internal sealed class ListBooksEndpoint : IEndpoint<BooksEndpointGroup, Book[]>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public string? RateLimitPolicy => Policies.Search;

    public Delegate Handler => Handle;

    private static Ok<Book[]> Handle([AsParameters] ListBooksQuery query, BookStore store) =>
        TypedResults.Ok(store.All
            .Where(book => query.Author is null
                           || book.Author.Contains(query.Author, StringComparison.OrdinalIgnoreCase))
            .ToArray());
}
