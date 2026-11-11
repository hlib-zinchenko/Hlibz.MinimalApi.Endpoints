using Hlibz.MinimalApi.Endpoints;

namespace Library.Api.Books;

/// <summary>
/// Uploads a CSV of <c>title,author</c> lines, up to 64 KB. Members only.
/// </summary>
internal sealed class ImportBooksEndpoint : IEndpoint<BooksEndpointGroup, int>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => "import";

    public long? MaxUploadBytes => 64 * 1024;

    public string? AuthorizationPolicy => Policies.Member;

    public Delegate Handler => HandleAsync;

    private static async Task<IResult> HandleAsync(
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
