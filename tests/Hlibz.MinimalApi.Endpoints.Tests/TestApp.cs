using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hlibz.MinimalApi.Endpoints.Tests;

/// <summary>
/// An in-memory app mapping the endpoints of one fixture namespace.
/// </summary>
internal static class TestApp
{
    public static async Task<WebApplication> StartAsync(
        string fixtureNamespace,
        Action<EndpointMappingOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        configureServices?.Invoke(builder.Services);

        WebApplication app = builder.Build();
        app.MapEndpoints(
            typeof(TestApp).Assembly,
            options =>
            {
                options.IncludeEndpoint = type => InNamespace(type, fixtureNamespace);
                configure?.Invoke(options);
            });

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    public static RouteEndpoint Endpoint(this WebApplication app, string name)
    {
        return app.FindEndpoint(name)
               ?? throw new InvalidOperationException($"No endpoint is named '{name}'.");
    }

    public static RouteEndpoint? FindEndpoint(this WebApplication app, string name)
    {
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SingleOrDefault(endpoint =>
                endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
    }

    public static IProducesResponseTypeMetadata[] Responses(
        this RouteEndpoint endpoint,
        int statusCode)
    {
        return
        [
            .. endpoint.Metadata
                .GetOrderedMetadata<IProducesResponseTypeMetadata>()
                .Where(response => response.StatusCode == statusCode),
        ];
    }

    public static bool InNamespace(Type type, string fixtureNamespace) =>
        type.Namespace is { } name
        && (name == fixtureNamespace
            || name.StartsWith(fixtureNamespace + ".", StringComparison.Ordinal));
}
