namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// A route group: the prefix and shared rules of the endpoints that name it in
/// <see cref="IEndpoint{TGroup}"/>. <c>MapEndpoints</c> creates it with its parameterless
/// constructor (public, or internal to the endpoints' assembly) and maps it once, with
/// <c>MapGroup</c>.
/// </summary>
/// <remarks>
/// Only the prefix is a property, because group conventions read it before any endpoint is
/// mapped. Everything else (<c>WithTags</c>, <c>RequireAuthorization</c>,
/// <c>RequireRateLimiting</c>, <c>WithGroupName</c>, ...) goes in <see cref="Configure"/>, as a
/// plain minimal API chain on the group.
/// </remarks>
public interface IEndpointGroup
{
    /// <summary>
    /// Gets the route prefix of every endpoint in the group, e.g. <c>"api/orders"</c>.
    /// </summary>
    [StringSyntax("Route")]
    string Prefix { get; }

    /// <summary>
    /// Applies the group's rules to every endpoint in it, e.g.
    /// <c>group.WithTags("Orders").RequireAuthorization("Member")</c>. Runs once, after
    /// <c>MapGroup</c> and before the <see cref="EndpointMappingOptions.GroupConventions"/>.
    /// </summary>
    /// <param name="group">The group's route group builder.</param>
    void Configure(RouteGroupBuilder group)
    {
        // A group is just its prefix by default.
    }
}
