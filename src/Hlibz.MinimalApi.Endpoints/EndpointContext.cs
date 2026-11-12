namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// An endpoint being mapped, as a <see cref="EndpointMappingOptions.EndpointConventions"/>
/// convention (or an add-on such as request validation) sees it.
/// </summary>
public sealed class EndpointContext
{
    internal EndpointContext(
        EndpointDescriptor descriptor,
        EndpointGroupDescriptor group,
        RouteHandlerBuilder builder,
        IServiceProvider services,
        EndpointMappingOptions options)
    {
        Descriptor = descriptor;
        GroupDescriptor = group;
        Builder = builder;
        Services = services;
        Options = options;
    }

    /// <summary>
    /// Gets the discovered endpoint.
    /// </summary>
    public EndpointDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the endpoint instance.
    /// </summary>
    public IEndpoint Endpoint => Descriptor.Endpoint;

    /// <summary>
    /// Gets the endpoint type.
    /// </summary>
    public Type EndpointType => Descriptor.EndpointType;

    /// <summary>
    /// Gets the discovered group the endpoint belongs to.
    /// </summary>
    public EndpointGroupDescriptor GroupDescriptor { get; }

    /// <summary>
    /// Gets the group instance the endpoint belongs to.
    /// </summary>
    public IEndpointGroup Group => GroupDescriptor.Group;

    /// <summary>
    /// Gets the endpoint's route handler builder.
    /// </summary>
    public RouteHandlerBuilder Builder { get; }

    /// <summary>
    /// Gets the application's root service provider.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the mapping options.
    /// </summary>
    public EndpointMappingOptions Options { get; }

    /// <summary>
    /// Documents an error response with the configured error schema
    /// (<see cref="EndpointMappingOptions.ErrorResponseType"/>), as
    /// <see cref="EndpointResponseExtensions.ProducesErrors"/> does inside an endpoint's
    /// <see cref="IEndpoint.Map"/>.
    /// </summary>
    /// <remarks>
    /// Call it while the convention runs, during <c>MapEndpoints</c>. Inside a
    /// <c>Builder.Add(...)</c> callback, which runs later, add a
    /// <see cref="ProducesErrorMetadata"/> to the endpoint builder's metadata instead.
    /// </remarks>
    /// <param name="statusCode">The error status code.</param>
    /// <returns>The context, for chaining.</returns>
    public EndpointContext ProducesError(int statusCode)
    {
        Builder.WithMetadata(new ProducesErrorMetadata(statusCode));
        return this;
    }
}
