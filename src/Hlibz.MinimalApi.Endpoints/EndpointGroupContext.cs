namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// A route group being mapped, as a <see cref="EndpointMappingOptions.GroupConventions"/>
/// convention sees it.
/// </summary>
public sealed class EndpointGroupContext
{
    internal EndpointGroupContext(
        EndpointGroupDescriptor descriptor,
        RouteGroupBuilder builder,
        IServiceProvider services,
        EndpointMappingOptions options)
    {
        Descriptor = descriptor;
        Builder = builder;
        Services = services;
        Options = options;
    }

    /// <summary>
    /// Gets the discovered group and its endpoints.
    /// </summary>
    public EndpointGroupDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the group instance.
    /// </summary>
    public IEndpointGroup Group => Descriptor.Group;

    /// <summary>
    /// Gets the group type.
    /// </summary>
    public Type GroupType => Descriptor.GroupType;

    /// <summary>
    /// Gets the group's route group builder.
    /// </summary>
    public RouteGroupBuilder Builder { get; }

    /// <summary>
    /// Gets the application's root service provider.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the mapping options.
    /// </summary>
    public EndpointMappingOptions Options { get; }
}
