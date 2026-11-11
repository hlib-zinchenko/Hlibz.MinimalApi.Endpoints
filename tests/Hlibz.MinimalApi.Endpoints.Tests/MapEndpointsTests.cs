using System.Net;
using System.Net.Http.Json;

using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace Hlibz.MinimalApi.Endpoints.Tests;

public sealed class MapEndpointsTests
{
    private const string Orders = "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders";

    [Fact]
    public async Task MapEndpoints_WithGroupPrefix_ServesEndpointUnderPrefix()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        using HttpClient client = app.GetTestClient();

        OrderDto? order = await client.GetFromJsonAsync<OrderDto>(
            "api/orders/5",
            TestContext.Current.CancellationToken);

        Assert.Equal(new OrderDto(5), order);
    }

    [Fact]
    public async Task MapEndpoints_WithEndpointMethod_AnswersOnlyThatMethod()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.DeleteAsync(
            "api/orders/5",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task MapEndpoints_ByDefault_NamesEndpointAfterTypeWithoutSuffix()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        RouteEndpoint endpoint = app.Endpoint("GetOrder");

        Assert.Equal("api/orders/{id:int}", endpoint.RoutePattern.RawText);
    }

    [Fact]
    public async Task MapEndpoints_WithCustomEndpointName_UsesIt()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.EndpointName = type => "Orders_" + type.Name);

        RouteEndpoint endpoint = app.Endpoint("Orders_GetOrderEndpoint");

        Assert.NotNull(endpoint);
    }

    [Fact]
    public async Task MapEndpoints_WithNullEndpointName_LeavesEndpointsUnnamed()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.EndpointName = _ => null);

        IEnumerable<Endpoint> endpoints =
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints);

        Assert.All(
            endpoints,
            endpoint => Assert.Null(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

    [Fact]
    public async Task MapEndpoints_WithGroupTags_TagsEveryEndpoint()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        ITagsMetadata? tags = app.Endpoint("GetOrder").Metadata.GetMetadata<ITagsMetadata>();

        Assert.Equal(["Orders"], tags?.Tags);
    }

    [Fact]
    public async Task MapEndpoints_WithResponseType_DocumentsSuccessResponse()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("GetOrder").Responses(StatusCodes.Status200OK));

        Assert.Equal(typeof(OrderDto), response.Type);
    }

    [Fact]
    public async Task MapEndpoints_WithSuccessStatusCode_DocumentsResponseUnderIt()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("CreateOrder");

        IProducesResponseTypeMetadata response =
            Assert.Single(endpoint.Responses(StatusCodes.Status201Created));

        Assert.Equal(typeof(OrderDto), response.Type);
        Assert.Empty(endpoint.Responses(StatusCodes.Status200OK));
    }

    [Fact]
    public async Task MapEndpoints_WithProducesErrors_DocumentsThemAsProblemDetails()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("GetOrder").Responses(StatusCodes.Status404NotFound));

        Assert.Equal(typeof(ProblemDetails), response.Type);
        Assert.Equal(["application/problem+json"], response.ContentTypes);
    }

    [Fact]
    public async Task MapEndpoints_WithOwnErrorResponse_DocumentsErrorsWithIt()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.UseErrorResponse<TestError>());

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("GetOrder").Responses(StatusCodes.Status404NotFound));

        Assert.Equal(typeof(TestError), response.Type);
        Assert.Equal(["application/json"], response.ContentTypes);
    }

    [Fact]
    public async Task MapEndpoints_WithoutErrorResponseType_DocumentsErrorsWithoutBody()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.ErrorResponseType = null);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("GetOrder").Responses(StatusCodes.Status404NotFound));

        Assert.Null(response.Type);
    }

    [Fact]
    public async Task MapEndpoints_WithFileContentType_DocumentsFileResponse()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("ExportOrders").Responses(StatusCodes.Status200OK));

        Assert.Equal(typeof(byte[]), response.Type);
        Assert.Equal(["text/csv"], response.ContentTypes);
    }

    [Fact]
    public async Task MapEndpoints_WithMaxUploadBytes_LimitsBodyAndDocuments413()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("ImportOrders");

        Assert.Equal(
            1024,
            endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize);
        Assert.False(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        Assert.Single(endpoint.Responses(StatusCodes.Status413PayloadTooLarge));
    }

    [Fact]
    public async Task MapEndpoints_WithAuthorizationPolicy_RequiresItAndDocuments401And403()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("MyOrders");

        IAuthorizeData authorize = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.Equal("Member", authorize.Policy);
        Assert.Single(endpoint.Responses(StatusCodes.Status401Unauthorized));
        Assert.Single(endpoint.Responses(StatusCodes.Status403Forbidden));
    }

    [Fact]
    public async Task MapEndpoints_WithoutAuthorization_DocumentsNo401()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        Assert.Empty(app.Endpoint("GetOrder").Responses(StatusCodes.Status401Unauthorized));
    }

    [Fact]
    public async Task MapEndpoints_WithAuthorizationResponsesOff_DocumentsNo403()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.DocumentAuthorizationResponses = false);

        Assert.Empty(app.Endpoint("MyOrders").Responses(StatusCodes.Status403Forbidden));
    }

    [Fact]
    public async Task MapEndpoints_WithRateLimitPolicy_RequiresItAndDocuments429()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("SearchOrders");

        Assert.Equal(
            "Search",
            endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Single(endpoint.Responses(StatusCodes.Status429TooManyRequests));
    }

    [Fact]
    public async Task MapEndpoints_WithRateLimitResponsesOff_DocumentsNo429()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.DocumentRateLimitResponses = false);

        Assert.Empty(app.Endpoint("SearchOrders").Responses(StatusCodes.Status429TooManyRequests));
    }

    [Fact]
    public async Task MapEndpoints_WithConfigure_AppliesEndpointsOwnSettings()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IEndpointSummaryMetadata? summary =
            app.Endpoint("CountOrders").Metadata.GetMetadata<IEndpointSummaryMetadata>();

        Assert.Equal("Counts the orders.", summary?.Summary);
    }

    [Fact]
    public async Task MapEndpoints_WithEndpointConvention_AppliesItToEveryEndpoint()
    {
        List<string> seen = [];

        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.ConfigureEndpoints(context =>
            {
                seen.Add(context.EndpointType.Name);
                context.Builder.WithDescription(context.Group.Prefix);
                context.ProducesError(StatusCodes.Status409Conflict);
            }));

        Assert.Equal(7, seen.Count);
        RouteEndpoint endpoint = app.Endpoint("GetOrder");
        Assert.Equal(
            "api/orders",
            endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
        Assert.Single(endpoint.Responses(StatusCodes.Status409Conflict));
    }

    [Fact]
    public async Task MapEndpoints_WithErrorAlsoDeclaredByEndpoint_DocumentsItOnce()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        Assert.Single(app.Endpoint("MyOrders").Responses(StatusCodes.Status401Unauthorized));
    }

    [Fact]
    public async Task MapEndpoints_WithIncludeEndpoint_MapsOnlyIncludedEndpoints()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Orders,
            options => options.IncludeEndpoint = type => type == typeof(GetOrderEndpoint));

        Endpoint endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints));

        Assert.Equal("GetOrder", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
    }

    private sealed record TestError(string Message);
}
