namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// An endpoint found by <see cref="EndpointDiscovery"/>.
/// </summary>
/// <param name="EndpointType">The endpoint type.</param>
/// <param name="Endpoint">The endpoint instance.</param>
/// <param name="GroupType">The group named in <see cref="IEndpoint{TGroup}"/>.</param>
/// <param name="ResponseType">
/// The success response named in <see cref="IEndpoint{TGroup, TResponse}"/>, or
/// <see langword="null"/>.
/// </param>
public sealed record EndpointDescriptor(
    Type EndpointType,
    IEndpoint Endpoint,
    Type GroupType,
    Type? ResponseType);

/// <summary>
/// A route group found by <see cref="EndpointDiscovery"/>, with its endpoints.
/// </summary>
/// <param name="GroupType">The group type.</param>
/// <param name="Group">The group instance.</param>
/// <param name="Endpoints">The group's endpoints, in discovery order.</param>
public sealed record EndpointGroupDescriptor(
    Type GroupType,
    IEndpointGroup Group,
    IReadOnlyList<EndpointDescriptor> Endpoints);
