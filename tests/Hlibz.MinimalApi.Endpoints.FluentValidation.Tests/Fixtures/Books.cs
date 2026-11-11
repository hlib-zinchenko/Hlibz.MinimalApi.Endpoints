using FluentValidation;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Tests.Fixtures;

internal sealed record AddBookRequest(string Title, int Pages);

internal sealed record BookQuery(string? Author);

internal sealed record Unvalidated(string Name);

internal sealed class AddBookRequestValidator : AbstractValidator<AddBookRequest>
{
    public AddBookRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty();
        RuleFor(request => request.Pages).GreaterThan(0);
    }
}

internal sealed class BookQueryValidator : AbstractValidator<BookQuery>
{
    public BookQueryValidator()
    {
        RuleFor(query => query.Author).MaximumLength(5);
    }
}

internal sealed class BooksEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/books";
}

internal sealed class AddBookEndpoint : IEndpoint<BooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => string.Empty;

    public Delegate Handler => ([FromBody] AddBookRequest request) => Results.Ok(request.Title);
}

internal sealed class SearchBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public Delegate Handler => ([AsParameters] BookQuery query) => Results.Ok(query.Author);
}

internal sealed class EchoEndpoint : IEndpoint<BooksEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Post;

    public string Route => "echo";

    public Delegate Handler => ([FromBody] Unvalidated body) => Results.Ok(body.Name);
}
