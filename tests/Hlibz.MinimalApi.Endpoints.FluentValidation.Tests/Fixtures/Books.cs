using FluentValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

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
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost(string.Empty, ([FromBody] AddBookRequest request) => Results.Ok(request.Title));
}

internal sealed class SearchBooksEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapGet(string.Empty, ([AsParameters] BookQuery query) => Results.Ok(query.Author));
}

internal sealed class EchoEndpoint : IEndpoint<BooksEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
        group.MapPost("echo", ([FromBody] Unvalidated body) => Results.Ok(body.Name));
}
