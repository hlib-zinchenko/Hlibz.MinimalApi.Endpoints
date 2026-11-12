using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hlibz.MinimalApi.Endpoints.FluentValidation.Tests.Fixtures;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Tests;

public sealed class AddFluentValidationTests
{
    [Fact]
    public async Task Filter_WithValidBody_RunsHandler()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books",
            new AddBookRequest("Dune", 412),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Filter_WithInvalidBody_AnswersValidationProblem()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books",
            new AddBookRequest(string.Empty, 0),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement errors = body.RootElement.GetProperty("errors");
        Assert.Equal(
            ["Title", "Pages"],
            errors.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task Filter_WithInvalidAsParameters_AnswersValidationProblem()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(
            "api/books?author=Frank%20Herbert",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Filter_WithInvalidBodyInsideAsParameters_AnswersValidationProblem()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books/copies?copies=2",
            new AddBookRequest(string.Empty, 0),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Single(app.Endpoint("AddBookCopies").Responses(StatusCodes.Status400BadRequest));
    }

    [Fact]
    public async Task Filter_WithValidBodyInsideAsParameters_RunsHandler()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books/copies?copies=2",
            new AddBookRequest("Dune", 412),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "2",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Frank", HttpStatusCode.OK)]
    [InlineData("Frank%20Herbert", HttpStatusCode.BadRequest)]
    public async Task Filter_WithTwoValidatedArguments_ValidatesBoth(
        string author,
        HttpStatusCode expected)
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"api/books/by-author?author={author}",
            new AddBookRequest("Dune", 412),
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Filter_WithCustomFailure_AnswersWithIt()
    {
        await using WebApplication app = await TestApp.StartAsync(options =>
        {
            options.OnFailure = (result, _) => Results.UnprocessableEntity(result.Errors.Count);
            options.FailureStatusCode = StatusCodes.Status422UnprocessableEntity;
        });
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books",
            new AddBookRequest(string.Empty, 0),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "2",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Filter_WithoutValidator_IsNotAttached()
    {
        await using WebApplication app = await TestApp.StartAsync();
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/books/echo",
            new Unvalidated(string.Empty),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(app.Endpoint("Echo").Responses(StatusCodes.Status400BadRequest));
    }

    [Fact]
    public async Task AddFluentValidation_ByDefault_DocumentsValidationProblem()
    {
        await using WebApplication app = await TestApp.StartAsync();

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("AddBook").Responses(StatusCodes.Status400BadRequest));

        Assert.Equal(typeof(HttpValidationProblemDetails), response.Type);
    }

    [Fact]
    public async Task AddFluentValidation_WithCustomFailure_DocumentsMappingErrorSchema()
    {
        await using WebApplication app = await TestApp.StartAsync(
            options =>
            {
                options.OnFailure = (_, _) => Results.BadRequest();
            },
            mapping => mapping.UseErrorResponse<TestError>());

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("AddBook").Responses(StatusCodes.Status400BadRequest));

        Assert.Equal(typeof(TestError), response.Type);
    }

    [Fact]
    public async Task AddFluentValidation_WithoutFailureStatusCode_DocumentsNothing()
    {
        await using WebApplication app = await TestApp.StartAsync(
            options => options.FailureStatusCode = null);

        Assert.Empty(app.Endpoint("AddBook").Responses(StatusCodes.Status400BadRequest));
    }

    [Fact]
    public void ToErrors_WithFailures_GroupsMessagesByProperty()
    {
        global::FluentValidation.Results.ValidationResult result =
            new AddBookRequestValidator().Validate(new AddBookRequest(string.Empty, 0));

        Dictionary<string, string[]> errors = FluentValidationEndpointOptions.ToErrors(result);

        Assert.Equal(["Title", "Pages"], errors.Keys);
        Assert.Single(errors["Title"]);
    }

    private sealed record TestError(string Message);
}
