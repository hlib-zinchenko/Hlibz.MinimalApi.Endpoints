namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// One minimal API endpoint, in a class of its own. Don't implement this interface directly:
/// implement <see cref="IEndpoint{TGroup}"/> or <see cref="IEndpoint{TGroup, TResponse}"/>, which
/// tie the endpoint to its <see cref="IEndpointGroup"/>.
/// <c>MapEndpoints</c> finds every implementation in the assemblies it is given, creates it with
/// its parameterless constructor and maps <see cref="Handler"/> into the group's route group.
/// </summary>
/// <remarks>
/// Endpoints are created once, at startup, so keep them stateless: the handler's dependencies come
/// in as its own parameters (<c>[FromServices]</c>, <c>[FromBody]</c>, <c>[AsParameters]</c>, ...)
/// exactly as with <c>MapGet</c>/<c>MapPost</c>.
/// </remarks>
public interface IEndpoint
{
    /// <summary>
    /// Gets the HTTP method the endpoint answers, e.g. <see cref="HttpMethod.Get"/>.
    /// </summary>
    HttpMethod Method { get; }

    /// <summary>
    /// Gets the route, relative to the group's <see cref="IEndpointGroup.Prefix"/>:
    /// <see cref="string.Empty"/> for the group root, or e.g. <c>"{id}"</c>.
    /// </summary>
    string Route { get; }

    /// <summary>
    /// Gets the route handler: the same delegate you would pass to <c>MapGet</c>/<c>MapPost</c>.
    /// </summary>
    Delegate Handler { get; }

    /// <summary>
    /// Gets the status code of the success response that <see cref="IEndpoint{TGroup, TResponse}"/>
    /// and <see cref="FileContentType"/> document. Defaults to <c>200</c>.
    /// </summary>
    int SuccessStatusCode => StatusCodes.Status200OK;

    /// <summary>
    /// Gets the error status codes the endpoint can answer with, each documented with the error
    /// schema of <see cref="EndpointMappingOptions.ErrorResponseType"/>. Leave out
    /// <c>401</c>/<c>403</c>/<c>429</c>/<c>413</c>: they are documented automatically when the
    /// endpoint requires authorization, is rate limited or takes an upload.
    /// </summary>
    IReadOnlyList<int> ProducesErrors => [];

    /// <summary>
    /// Gets the authorization policy the endpoint requires, or <see langword="null"/> to leave
    /// it to the group (or open to anonymous callers).
    /// </summary>
    string? AuthorizationPolicy => null;

    /// <summary>
    /// Gets the rate limiting policy the endpoint is subject to, or <see langword="null"/> for
    /// none of its own.
    /// </summary>
    string? RateLimitPolicy => null;

    /// <summary>
    /// Gets the largest request body, in bytes, the endpoint accepts, or <see langword="null"/>
    /// for an endpoint that takes no file upload. Setting it marks the endpoint as a file upload:
    /// the limit is applied to the request, antiforgery is switched off (for token-authenticated
    /// APIs; leave this <see langword="null"/> and configure the endpoint yourself when cookies
    /// authenticate it), and <c>413</c> is documented.
    /// </summary>
    long? MaxUploadBytes => null;

    /// <summary>
    /// Gets the content type of the file the endpoint returns on success, or
    /// <see langword="null"/> for an endpoint that returns JSON. Documents the success response as
    /// a file of that content type rather than a JSON schema.
    /// </summary>
    string? FileContentType => null;

    /// <summary>
    /// Applies anything else to the endpoint's route, after everything the interface describes
    /// and before the <see cref="EndpointMappingOptions.EndpointConventions"/>.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    void Configure(RouteHandlerBuilder builder)
    {
        // Nothing beyond the interface's own members by default.
    }
}

/// <summary>
/// An endpoint mapped into the route group <typeparamref name="TGroup"/>.
/// </summary>
/// <typeparam name="TGroup">The group the endpoint belongs to.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Read via reflection to map the endpoint into its route group.")]
public interface IEndpoint<TGroup> : IEndpoint
    where TGroup : IEndpointGroup
{
}

/// <summary>
/// An endpoint mapped into the route group <typeparamref name="TGroup"/> whose success response
/// (<see cref="IEndpoint.SuccessStatusCode"/>) is documented as <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TGroup">The group the endpoint belongs to.</typeparam>
/// <typeparam name="TResponse">The success response body.</typeparam>
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Read via reflection to document the endpoint's success response.")]
public interface IEndpoint<TGroup, TResponse> : IEndpoint<TGroup>
    where TGroup : IEndpointGroup
{
}
