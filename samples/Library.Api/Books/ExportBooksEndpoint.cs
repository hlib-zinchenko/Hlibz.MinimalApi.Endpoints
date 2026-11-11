using System.Text;

using Hlibz.MinimalApi.Endpoints;

using Microsoft.AspNetCore.Http.HttpResults;

namespace Library.Api.Books;

/// <summary>
/// Downloads the catalog as CSV.
/// </summary>
internal sealed class ExportBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "export";

    public string? FileContentType => "text/csv";

    public Delegate Handler => Handle;

    private static FileContentHttpResult Handle(BookStore store)
    {
        StringBuilder csv = new("id,title,author\n");
        foreach (Book book in store.All)
        {
            csv.Append(book.Id).Append(',').Append(book.Title).Append(',').Append(book.Author)
                .Append('\n');
        }

        return TypedResults.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "books.csv");
    }
}
