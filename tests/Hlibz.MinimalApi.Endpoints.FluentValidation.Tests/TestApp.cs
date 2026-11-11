using FluentValidation;

using Hlibz.MinimalApi.Endpoints.FluentValidation.Tests.Fixtures;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Tests;

internal static class TestApp
{
    public static async Task<WebApplication> StartAsync(
        Action<FluentValidationEndpointOptions>? configure = null,
        Action<EndpointMappingOptions>? configureMapping = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        // Scoped, as AddValidatorsFromAssembly registers them: resolving one from the root
        // provider at startup would fail scope validation in Development.
        builder.Services.AddScoped<IValidator<AddBookRequest>, AddBookRequestValidator>();
        builder.Services.AddScoped<IValidator<BookQuery>, BookQueryValidator>();

        WebApplication app = builder.Build();
        app.MapEndpoints(
            typeof(TestApp).Assembly,
            options =>
            {
                configureMapping?.Invoke(options);
                options.AddFluentValidation(configure);
            });

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    public static RouteEndpoint Endpoint(this WebApplication app, string name)
    {
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint =>
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
}
