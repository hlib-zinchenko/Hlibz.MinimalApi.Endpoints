using Hlibz.MinimalApi.Endpoints.Internal;

using Microsoft.AspNetCore.Http.Metadata;

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
        "Each endpoint's Map binds its handler through RequestDelegateFactory unless ASP.NET "
        + "Core's Request Delegate Generator intercepted its MapGet/MapPost/... call: it reflects "
        + "on the handler's parameters at startup.";

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
    /// (<c>MapGroup</c> with its <see cref="IEndpointGroup.Prefix"/>, then
    /// <see cref="IEndpointGroup.Configure"/> and the group conventions), then each endpoint
    /// (its own <see cref="IEndpoint.Map"/>, then its name (<see cref="IEndpoint.Name"/>, else
    /// <see cref="EndpointMappingOptions.EndpointName"/>), then the endpoint conventions). The
    /// documented error, authorization and rate limiting responses, and the description from the
    /// endpoint's XML summary, are added once each endpoint's metadata is final.
    /// </summary>
    /// <param name="app">The app to map the endpoints on.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="configure">Configures the mapping; the defaults when omitted.</param>
    /// <returns>The same <paramref name="app"/>, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// An endpoint or group can't be used, two endpoints get the same name, an endpoint's
    /// <see cref="IEndpoint.Map"/> returned <see langword="null"/>, or a convention rejected one.
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
        group.Configure(builder);

        EndpointGroupContext context = new(descriptor, builder, app.ServiceProvider, options);
        foreach (Action<EndpointGroupContext> convention in options.GroupConventions)
        {
            convention(context);
        }

        return builder;
    }

    private static void MapEndpoint(
        IEndpointRouteBuilder app,
        RouteGroupBuilder groupBuilder,
        EndpointGroupDescriptor group,
        EndpointDescriptor descriptor,
        EndpointMappingOptions options,
        Dictionary<string, Type> names)
    {
        IEndpoint endpoint = descriptor.Endpoint;
        string? name = endpoint.Name ?? options.EndpointName(descriptor.EndpointType);
        if (name is not null && !names.TryAdd(name, descriptor.EndpointType))
        {
            throw new InvalidOperationException(
                $"{names[name].FullName} and {descriptor.EndpointType.FullName} are both named "
                + $"'{name}'. Give one its own {nameof(IEndpoint)}.{nameof(IEndpoint.Name)}.");
        }

        // Null from an endpoint compiled without nullable checks, or one that suppressed them.
        RouteHandlerBuilder builder = (RouteHandlerBuilder?)endpoint.Map(groupBuilder)
            ?? throw new InvalidOperationException(
                $"{descriptor.EndpointType.FullName}.{nameof(IEndpoint.Map)} returned null; "
                + "return the builder its MapGet/MapPost/... call returned.");

        if (name is not null)
        {
            builder.WithName(name);
        }

        EndpointContext context = new(descriptor, group, builder, app.ServiceProvider, options);
        foreach (Action<EndpointContext> convention in options.EndpointConventions)
        {
            convention(context);
        }

        if (options.DescribeEndpointsFromSummaries && descriptor.Description is { } description)
        {
            builder.Finally(endpointBuilder =>
            {
                if (!endpointBuilder.Metadata.OfType<IEndpointDescriptionMetadata>().Any())
                {
                    endpointBuilder.Metadata.Add(new EndpointDescriptionAttribute(description));
                }
            });
        }

        ResponseDocumentation.Document(builder, descriptor.ResponseType, options);
    }
}
