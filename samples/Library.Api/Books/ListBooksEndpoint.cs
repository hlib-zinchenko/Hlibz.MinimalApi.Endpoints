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
/// Lists the catalog, optionally only one author's books. Rate limited; an author name over
/// 50 characters is a <c>400</c>.
/// </summary>
internal sealed class ListBooksEndpoint : IEndpoint<BooksEndpointGroup, Book[]>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet(string.Empty, Handle)
            .RequireRateLimiting(Policies.Search);

    private static Ok<Book[]> Handle([AsParameters] ListBooksQuery query, BookStore store) =>
        TypedResults.Ok(store.All
            .Where(book => query.Author is null
                           || book.Author.Contains(query.Author, StringComparison.OrdinalIgnoreCase))
            .ToArray());
}
