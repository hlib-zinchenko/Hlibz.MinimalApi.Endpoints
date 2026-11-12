namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// Endpoint metadata asking <c>MapEndpoints</c> to document <see cref="StatusCode"/> with the
/// mapping's error schema (<see cref="EndpointMappingOptions.ErrorResponseType"/>). Added by
/// <see cref="EndpointResponseExtensions.ProducesErrors"/> and
/// <see cref="EndpointContext.ProducesError"/>; add it yourself only from a convention that works
/// on <see cref="EndpointBuilder.Metadata"/> directly.
/// </summary>
/// <remarks>
/// Resolved once the endpoint's metadata is final: it replaces a body-less response ASP.NET
/// inferred for the same status (a <c>NotFound</c> arm of <c>Results&lt;...&gt;</c>), leaves a
/// response documented with a body alone, and is then removed from the metadata.
/// </remarks>
/// <param name="statusCode">The error status code.</param>
public sealed class ProducesErrorMetadata(int statusCode)
{
    /// <summary>
    /// Gets the error status code.
    /// </summary>
    public int StatusCode { get; } = statusCode;
}
