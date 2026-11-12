using Microsoft.AspNetCore.Mvc;

namespace Hlibz.MinimalApi.Endpoints;

/// <summary>
/// What an endpoint's <see cref="IEndpoint.Map"/> chains to document responses the handler's
/// return type doesn't, and to take file uploads.
/// </summary>
public static class EndpointResponseExtensions
{
    /// <summary>
    /// Documents error responses with the mapping's error schema
    /// (<see cref="EndpointMappingOptions.ErrorResponseType"/>). Leave out
    /// <c>401</c>/<c>403</c>/<c>429</c>: they are documented whenever the endpoint ends up
    /// requiring authorization or being rate limited.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    /// <param name="statusCodes">The error status codes.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static RouteHandlerBuilder ProducesErrors(
        this RouteHandlerBuilder builder,
        params int[] statusCodes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(statusCodes);

        foreach (int statusCode in statusCodes.Distinct())
        {
            builder.WithMetadata(new ProducesErrorMetadata(statusCode));
        }

        return builder;
    }

    /// <summary>
    /// Documents the success response as a file of <paramref name="contentType"/> rather than a
    /// JSON schema.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    /// <param name="contentType">The file's content type, e.g. <c>text/csv</c>.</param>
    /// <param name="statusCode">The success status code. Defaults to <c>200</c>.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static RouteHandlerBuilder ProducesFile(
        this RouteHandlerBuilder builder,
        string contentType,
        int statusCode = StatusCodes.Status200OK)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return builder.Produces<byte[]>(statusCode, contentType);
    }

    /// <summary>
    /// Marks the endpoint as a file upload: the request body, and a multipart form read from it,
    /// are limited to <paramref name="maxBytes"/> (replacing the form's own 128 MB default),
    /// antiforgery is switched off, and <c>413</c> is documented with
    /// the mapping's error schema. Switching antiforgery off suits token-authenticated APIs; when
    /// cookies authenticate the endpoint, configure it yourself instead.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    /// <param name="maxBytes">The largest request body, in bytes.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static RouteHandlerBuilder AcceptsUpload(this RouteHandlerBuilder builder, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfNegative(maxBytes);

        // Form reading has a limit of its own (128 MB by default) that would cut a larger
        // upload short.
        return builder
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(maxBytes))
            .WithFormOptions(multipartBodyLengthLimit: maxBytes)
            .WithMetadata(new ProducesErrorMetadata(StatusCodes.Status413PayloadTooLarge));
    }
}
