namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// One minimal API endpoint, in a class of its own. Don't implement this interface directly:
/// implement <see cref="IEndpoint{TGroup}"/>, which ties the endpoint to its
/// <see cref="IEndpointGroup"/>.
/// The package's source generator lists every implementation when the assembly compiles, and
/// <c>MapEndpoints</c> creates each one with its parameterless constructor (public or internal)
/// and calls <see cref="Map"/> with the group's route group.
/// </summary>
/// <remarks>
/// <see cref="Map"/> is a plain minimal API chain: one <c>MapGet</c>/<c>MapPost</c>/... call,
/// then anything else a <see cref="RouteHandlerBuilder"/> takes (<c>RequireAuthorization</c>,
/// <c>RequireRateLimiting</c>, <c>WithSummary</c>, the package's own
/// <see cref="EndpointResponseExtensions.ProducesErrors"/>, ...). Endpoints are created once, at
/// startup, so keep them stateless: the handler's dependencies come in as its own parameters
/// (<c>[FromServices]</c>, <c>[FromBody]</c>, <c>[AsParameters]</c>, ...).
/// </remarks>
public interface IEndpoint
{
    /// <summary>
    /// Gets the endpoint's route name (<c>WithName</c>, which is also its OpenAPI operation id),
    /// or <see langword="null"/> to name it with <see cref="EndpointMappingOptions.EndpointName"/>
    /// (by default the class name minus a trailing <c>Endpoint</c>). Set it when two endpoint
    /// classes share a name, e.g. <c>GetBookEndpoint</c> in a books group and an admin group.
    /// A <c>WithName</c> call inside <see cref="Map"/> is overridden by this name, or by the
    /// naming option when this is <see langword="null"/>.
    /// </summary>
    string? Name => null;

    /// <summary>
    /// Maps the endpoint's route on <paramref name="group"/>: exactly one call to
    /// <c>MapGet</c>, <c>MapPost</c>, <c>MapPut</c>, <c>MapDelete</c>, <c>MapPatch</c> or
    /// <c>MapMethods</c>, with anything else chained on the builder it returns. The route is
    /// relative to the group's <see cref="IEndpointGroup.Prefix"/>. One route per endpoint is
    /// checked at build time (<c>HMAE007</c>).
    /// </summary>
    /// <param name="group">The route group of the endpoint's <see cref="IEndpointGroup"/>.</param>
    /// <returns>The builder the <c>Map*</c> call returned.</returns>
    RouteHandlerBuilder Map(IEndpointRouteBuilder group);
}

/// <summary>
/// An endpoint mapped into the route group <typeparamref name="TGroup"/>.
/// </summary>
/// <typeparam name="TGroup">The group the endpoint belongs to.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Read by the source generator to map the endpoint into its route group.")]
public interface IEndpoint<TGroup> : IEndpoint
    where TGroup : IEndpointGroup
{
}
