using Hlibz.MinimalApi.Endpoints.Internal;

using Microsoft.AspNetCore.Mvc;

namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// Maps the endpoints <see cref="EndpointDiscovery"/> finds.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Why mapping isn't trimming- or AOT-safe, as a warning on the caller: the same as a
    /// <c>MapGet</c> given a delegate that ASP.NET Core's Request Delegate Generator can't see.
    /// </summary>
    private const string HandlerBindingWarning =
        "Each endpoint's Handler is bound through RequestDelegateFactory, like a MapMethods call "
        + "with a Delegate: it reflects on the handler's parameters at startup.";

    /// <summary>
    /// Maps every endpoint in <paramref name="assembly"/> into its route group.
    /// </summary>
    /// <param name="app">The app to map the endpoints on.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="configure">Configures the mapping; the defaults when omitted.</param>
    /// <returns>The same <paramref name="app"/>, for chaining.</returns>
    [RequiresUnreferencedCode(HandlerBindingWarning)]
    [RequiresDynamicCode(HandlerBindingWarning)]
    public static IEndpointRouteBuilder MapEndpoints(
        this IEndpointRouteBuilder app,
        Assembly assembly,
        Action<EndpointMappingOptions>? configure = null)
    {
        return app.MapEndpoints([assembly], configure);
    }

    /// <summary>
    /// Maps every endpoint in <paramref name="assemblies"/> into its route group: each group once
    /// (<c>MapGroup</c> with its prefix, tags, authorization and rate limiting, then
    /// <see cref="IEndpointGroup.Configure"/> and the group conventions), then each endpoint
    /// (its method, route, handler and name, its documented responses, authorization, rate
    /// limiting and upload limit, then <see cref="IEndpoint.Configure"/> and the endpoint
    /// conventions).
    /// </summary>
    /// <param name="app">The app to map the endpoints on.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="configure">Configures the mapping; the defaults when omitted.</param>
    /// <returns>The same <paramref name="app"/>, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// An endpoint or group can't be used, two endpoints get the same name, or a convention
    /// rejected one.
    /// </exception>
    [RequiresUnreferencedCode(HandlerBindingWarning)]
    [RequiresDynamicCode(HandlerBindingWarning)]
    public static IEndpointRouteBuilder MapEndpoints(
        this IEndpointRouteBuilder app,
        IEnumerable<Assembly> assemblies,
        Action<EndpointMappingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assemblies);

        EndpointMappingOptions options = new();
        configure?.Invoke(options);

        Dictionary<string, Type> names = new(StringComparer.Ordinal);
        foreach (EndpointGroupDescriptor group in
                 EndpointDiscovery.Discover(assemblies, options.IncludeEndpoint))
        {
            RouteGroupBuilder groupBuilder = MapGroup(app, group, options);
            foreach (EndpointDescriptor endpoint in group.Endpoints)
            {
                MapEndpoint(app, groupBuilder, group, endpoint, options, names);
            }
        }

        return app;
    }

    private static RouteGroupBuilder MapGroup(
        IEndpointRouteBuilder app,
        EndpointGroupDescriptor descriptor,
        EndpointMappingOptions options)
    {
        IEndpointGroup group = descriptor.Group;
        RouteGroupBuilder builder = app.MapGroup(group.Prefix);

        if (group.Tags.Count > 0)
        {
            builder.WithTags([.. group.Tags]);
        }

        if (group.AuthorizationPolicy is not null)
        {
            builder.RequireAuthorization(group.AuthorizationPolicy);
        }

        if (group.RateLimitPolicy is not null)
        {
            builder.RequireRateLimiting(group.RateLimitPolicy);
        }

        group.Configure(builder);

        EndpointGroupContext context = new(descriptor, builder, app.ServiceProvider, options);
        foreach (Action<EndpointGroupContext> convention in options.GroupConventions)
        {
            convention(context);
        }

        return builder;
    }

    [RequiresUnreferencedCode(HandlerBindingWarning)]
    [RequiresDynamicCode(HandlerBindingWarning)]
    private static void MapEndpoint(
        IEndpointRouteBuilder app,
        RouteGroupBuilder groupBuilder,
        EndpointGroupDescriptor group,
        EndpointDescriptor descriptor,
        EndpointMappingOptions options,
        Dictionary<string, Type> names)
    {
        string? name = options.EndpointName(descriptor.EndpointType);
        if (name is not null && !names.TryAdd(name, descriptor.EndpointType))
        {
            throw new InvalidOperationException(
                $"{names[name].FullName} and {descriptor.EndpointType.FullName} are both named "
                + $"'{name}'. Rename one, or set {nameof(EndpointMappingOptions)}."
                + $"{nameof(EndpointMappingOptions.EndpointName)} to tell them apart.");
        }

        IEndpoint endpoint = descriptor.Endpoint;
        RouteHandlerBuilder builder = groupBuilder.MapMethods(
            endpoint.Route,
            [endpoint.Method.Method],
            endpoint.Handler);

        EndpointContext context = new(descriptor, group, builder, app.ServiceProvider, options);

        if (name is not null)
        {
            builder.WithName(name);
        }

        if (descriptor.ResponseType is not null)
        {
            builder.Produces(endpoint.SuccessStatusCode, descriptor.ResponseType);
        }

        if (endpoint.FileContentType is not null)
        {
            builder.Produces<byte[]>(endpoint.SuccessStatusCode, endpoint.FileContentType);
        }

        if (endpoint.MaxUploadBytes is { } maxUploadBytes)
        {
            builder
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(maxUploadBytes));
            context.ProducesError(StatusCodes.Status413PayloadTooLarge);
        }

        foreach (int statusCode in endpoint.ProducesErrors.Distinct())
        {
            context.ProducesError(statusCode);
        }

        if (endpoint.AuthorizationPolicy is not null)
        {
            builder.RequireAuthorization(endpoint.AuthorizationPolicy);
        }

        if (endpoint.RateLimitPolicy is not null)
        {
            builder.RequireRateLimiting(endpoint.RateLimitPolicy);
        }

        endpoint.Configure(builder);

        foreach (Action<EndpointContext> convention in options.EndpointConventions)
        {
            convention(context);
        }

        ResponseDocumentation.DocumentGateResponses(builder, options);
    }
}
