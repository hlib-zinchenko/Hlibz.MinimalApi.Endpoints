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

    private const string DuplicateNames =
        "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.DuplicateNames";

    private const string ExplicitNames =
        "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.ExplicitNames";

    private const string ChainNames = "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.ChainNames";

    private const string NullMap = "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.NullMap";

    private const string Descriptions =
        "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Descriptions";

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
    public async Task MapEndpoints_WithTwoEndpointsGivenSameName_FailsAtStartupNamingBoth()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TestApp.StartAsync(DuplicateNames));

        Assert.Contains(typeof(Fixtures.DuplicateNames.Books.GetBookEndpoint).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(Fixtures.DuplicateNames.Admin.GetBookEndpoint).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains("'GetBook'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapEndpoints_WithEndpointNameProperty_NamesEndpointWithIt()
    {
        await using WebApplication app = await TestApp.StartAsync(ExplicitNames);

        Assert.Equal("api/admin/books/{id}", app.Endpoint("GetAdminBook").RoutePattern.RawText);
        Assert.Equal("api/books/{id}", app.Endpoint("GetBook").RoutePattern.RawText);
    }

    [Fact]
    public async Task MapEndpoints_WithWithNameInMapAndNameProperty_PrefersProperty()
    {
        await using WebApplication app = await TestApp.StartAsync(ExplicitNames);

        Assert.Equal(
            "GetAdminBook",
            app.Endpoint("GetAdminBook").Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
        Assert.Null(app.FindEndpoint("FromChain"));
    }

    [Fact]
    public async Task MapEndpoints_WithWithNameInMap_PrefersNamingOption()
    {
        await using WebApplication app = await TestApp.StartAsync(ChainNames);

        Assert.Equal("api/books/", app.Endpoint("ListBooks").RoutePattern.RawText);
        Assert.Null(app.FindEndpoint("FromChain"));
    }

    [Fact]
    public async Task MapEndpoints_WithMapReturningNull_FailsAtStartupNamingEndpoint()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TestApp.StartAsync(NullMap));

        Assert.StartsWith(
            typeof(Fixtures.NullMap.BrokenEndpoint).FullName + ".Map returned null",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapEndpoints_WithEndpointNamePropertyAndNamingOption_PrefersProperty()
    {
        await using WebApplication app = await TestApp.StartAsync(
            ExplicitNames,
            options => options.EndpointName = _ => null);

        Assert.Equal("api/admin/books/{id}", app.Endpoint("GetAdminBook").RoutePattern.RawText);
    }

    [Fact]
    public async Task MapEndpoints_WithEndpointNameTellingSameNamedTypesApart_MapsBoth()
    {
        await using WebApplication app = await TestApp.StartAsync(
            DuplicateNames,
            options => options.EndpointName = type => type.FullName);

        Assert.NotNull(app.Endpoint(typeof(Fixtures.DuplicateNames.Books.GetBookEndpoint).FullName!));
        Assert.NotNull(app.Endpoint(typeof(Fixtures.DuplicateNames.Admin.GetBookEndpoint).FullName!));
    }

    [Fact]
    public async Task MapEndpoints_WithGroupTags_TagsEveryEndpoint()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        ITagsMetadata? tags = app.Endpoint("GetOrder").Metadata.GetMetadata<ITagsMetadata>();

        Assert.Equal(["Orders"], tags?.Tags);
    }

    [Fact]
    public async Task MapEndpoints_WithTypedHandler_DocumentsSuccessResponseOnce()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("GetOrder").Responses(StatusCodes.Status200OK));

        Assert.Equal(typeof(OrderDto), response.Type);
        Assert.Equal(["application/json"], response.ContentTypes);
    }

    [Fact]
    public async Task MapEndpoints_WithCreatedHandler_Documents201()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("CreateOrder");

        IProducesResponseTypeMetadata response =
            Assert.Single(endpoint.Responses(StatusCodes.Status201Created));

        Assert.Equal(typeof(OrderDto), response.Type);
        Assert.Empty(endpoint.Responses(StatusCodes.Status200OK));
    }

    [Fact]
    public async Task MapEndpoints_WithProducesErrorsOverBodylessInferredStatus_ReplacesIt()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("GetOrder");

        IProducesResponseTypeMetadata response =
            Assert.Single(endpoint.Responses(StatusCodes.Status404NotFound));

        Assert.Equal(typeof(ProblemDetails), response.Type);
        Assert.Equal(["application/problem+json"], response.ContentTypes);
        Assert.Empty(endpoint.Metadata.GetOrderedMetadata<ProducesErrorMetadata>());
    }

    [Fact]
    public async Task MapEndpoints_WithProducesErrorsOverStatusWithBody_LeavesIt()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("ReserveOrder").Responses(StatusCodes.Status409Conflict));

        Assert.Equal(typeof(OrderDto), response.Type);
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
    public async Task MapEndpoints_WithProducesFile_DocumentsFileResponse()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IProducesResponseTypeMetadata response =
            Assert.Single(app.Endpoint("ExportOrders").Responses(StatusCodes.Status200OK));

        Assert.Equal(typeof(byte[]), response.Type);
        Assert.Equal(["text/csv"], response.ContentTypes);
    }

    [Fact]
    public async Task MapEndpoints_WithAcceptsUpload_LimitsBodyAndDocuments413()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);
        RouteEndpoint endpoint = app.Endpoint("ImportOrders");

        Assert.Equal(
            1024,
            endpoint.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize);
        Assert.Equal(
            1024,
            endpoint.Metadata.GetMetadata<IFormOptionsMetadata>()?.MultipartBodyLengthLimit);
        Assert.False(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation);
        IProducesResponseTypeMetadata response =
            Assert.Single(endpoint.Responses(StatusCodes.Status413PayloadTooLarge));
        Assert.Equal(typeof(ProblemDetails), response.Type);
    }

    [Fact]
    public async Task MapEndpoints_WithRequireAuthorizationInMap_RequiresItAndDocuments401And403()
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
    public async Task MapEndpoints_WithRequireRateLimitingInMap_RequiresItAndDocuments429()
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
    public async Task MapEndpoints_WithChainInMap_AppliesEndpointsOwnSettings()
    {
        await using WebApplication app = await TestApp.StartAsync(Orders);

        IEndpointSummaryMetadata? summary =
            app.Endpoint("CountOrders").Metadata.GetMetadata<IEndpointSummaryMetadata>();

        Assert.Equal("Counts the orders.", summary?.Summary);
    }

    [Fact]
    public async Task MapEndpoints_WithSummary_DescribesOperationWithIt()
    {
        await using WebApplication app = await TestApp.StartAsync(Descriptions);

        Assert.Equal(
            "Lists the books, `20` per page.\n\nNewest first.",
            Description(app.Endpoint("ListBooks")));
    }

    [Fact]
    public async Task MapEndpoints_WithSummaryAndWithDescriptionInMap_PrefersMap()
    {
        await using WebApplication app = await TestApp.StartAsync(Descriptions);

        Assert.Equal("From Map.", Description(app.Endpoint("CountBooks")));
    }

    [Fact]
    public async Task MapEndpoints_WithoutSummary_LeavesOperationUndescribed()
    {
        await using WebApplication app = await TestApp.StartAsync(Descriptions);

        Assert.Null(Description(app.Endpoint("PingBooks")));
    }

    [Fact]
    public async Task MapEndpoints_WithDescriptionsFromSummariesOff_LeavesOperationUndescribed()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Descriptions,
            options => options.DescribeEndpointsFromSummaries = false);

        Assert.Null(Description(app.Endpoint("ListBooks")));
    }

    [Fact]
    public async Task MapEndpoints_WithSummaryAndDescribingConvention_PrefersConvention()
    {
        await using WebApplication app = await TestApp.StartAsync(
            Descriptions,
            options => options.ConfigureEndpoints(
                context => context.Builder.WithDescription("From the convention.")));

        Assert.Equal("From the convention.", Description(app.Endpoint("ListBooks")));
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

        Assert.Equal(8, seen.Count);
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

    private static string? Description(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description;

    private sealed record TestError(string Message);
}
