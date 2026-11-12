using Hlibz.MinimalApi.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Library.Api.Books;

/// <summary>
/// Imports books from an uploaded CSV of <c>title,author</c> lines, up to 64 KB, and answers
/// how many were added. Members only.
/// </summary>
internal sealed class ImportBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("import", HandleAsync)
            .AcceptsUpload(64 * 1024)
            .RequireAuthorization(Policies.Member);

    private static async Task<Ok<int>> HandleAsync(
        IFormFile file,
        BookStore store,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = new(file.OpenReadStream());
        int added = 0;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Split(',') is [var title, var author])
            {
                store.Add(title.Trim(), author.Trim());
                added++;
            }
        }

        return TypedResults.Ok(added);
    }
}
