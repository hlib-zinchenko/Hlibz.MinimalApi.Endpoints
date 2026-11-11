namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// A route group: the prefix, OpenAPI tags and shared rules of the endpoints that name it in
/// <see cref="IEndpoint{TGroup}"/>. <c>MapEndpoints</c> creates it with its parameterless
/// constructor and maps it once, with <c>MapGroup</c>.
/// </summary>
public interface IEndpointGroup
{
    /// <summary>
    /// Gets the route prefix of every endpoint in the group, e.g. <c>"api/orders"</c>.
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// Gets the OpenAPI tags of every endpoint in the group.
    /// </summary>
    IReadOnlyList<string> Tags => [];

    /// <summary>
    /// Gets the authorization policy every endpoint in the group requires, or
    /// <see langword="null"/> to leave it to each endpoint.
    /// </summary>
    string? AuthorizationPolicy => null;

    /// <summary>
    /// Gets the rate limiting policy every endpoint in the group is subject to, or
    /// <see langword="null"/> to leave it to each endpoint.
    /// </summary>
    string? RateLimitPolicy => null;

    /// <summary>
    /// Applies anything else to the group, after everything the interface describes and before the
    /// <see cref="EndpointMappingOptions.GroupConventions"/>.
    /// </summary>
    /// <param name="builder">The group's route group builder.</param>
    void Configure(RouteGroupBuilder builder)
    {
        // Nothing beyond the interface's own members by default.
    }
}
