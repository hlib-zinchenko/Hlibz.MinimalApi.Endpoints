using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;

namespace Hlibz.MinimalApi.Endpoints.Internal;

/// <summary>
/// Builds the documented responses once the endpoint's metadata is final: the declared errors
/// with the configured schema, and the authorization and rate limiting gates' responses.
/// </summary>
internal static class ResponseDocumentation
{
    /// <summary>
    /// Adds a <c>Finally</c> convention that, once the endpoint's metadata is final (whatever the
    /// endpoint, its group, a convention or an outer route group added), documents in order:
    /// the declared errors (<see cref="ProducesErrorMetadata"/>), then
    /// <c>401</c>/<c>403</c>/<c>429</c>.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    /// <param name="options">The mapping options.</param>
    public static void Document(RouteHandlerBuilder builder, EndpointMappingOptions options)
    {
        // Captured now: the options aren't read again once mapping is over.
        Type? errorType = options.ErrorResponseType;
        string errorContentType = options.ErrorContentType;
        bool documentAuthorization = options.DocumentAuthorizationResponses;
        bool documentRateLimit = options.DocumentRateLimitResponses;

        builder.Finally(endpoint =>
        {
            IList<object> metadata = endpoint.Metadata;

            DocumentErrors(metadata, errorType, errorContentType);

            if (documentAuthorization && RequiresAuthorization(metadata))
            {
                AddIfMissing(
                    metadata,
                    Error(StatusCodes.Status401Unauthorized, errorType, errorContentType));
                AddIfMissing(
                    metadata,
                    Error(StatusCodes.Status403Forbidden, errorType, errorContentType));
            }

            if (documentRateLimit && IsRateLimited(metadata))
            {
                AddIfMissing(
                    metadata,
                    Error(StatusCodes.Status429TooManyRequests, errorType, errorContentType));
            }
        });
    }

    internal static bool RequiresAuthorization(IList<object> metadata) =>
        metadata.Any(item => item is IAuthorizeData or AuthorizationPolicy)
        && !metadata.Any(item => item is IAllowAnonymous);

    internal static bool IsRateLimited(IList<object> metadata) =>
        metadata.Any(item => item is EnableRateLimitingAttribute)
        && !metadata.Any(item => item is DisableRateLimitingAttribute);

    private static ProducesResponseTypeMetadata Error(
        int statusCode,
        Type? errorType,
        string errorContentType)
    {
        return errorType is null
            ? new ProducesResponseTypeMetadata(statusCode)
            : new ProducesResponseTypeMetadata(statusCode, errorType, [errorContentType]);
    }

    /// <summary>
    /// Turns every <see cref="ProducesErrorMetadata"/> into a documented error: a body-less
    /// response for the same status (ASP.NET's inferred <c>NotFound</c>) gives way to it, a
    /// response with a body is left alone. The markers are removed afterwards.
    /// </summary>
    private static void DocumentErrors(IList<object> metadata, Type? errorType, string contentType)
    {
        int[] statusCodes =
        [
            .. metadata.OfType<ProducesErrorMetadata>().Select(item => item.StatusCode).Distinct(),
        ];
        if (statusCodes.Length == 0)
        {
            return;
        }

        foreach (int statusCode in statusCodes)
        {
            RemoveAll(
                metadata,
                item => item is IProducesResponseTypeMetadata response
                        && response.StatusCode == statusCode
                        && (response.Type is null || response.Type == typeof(void)));
            AddIfMissing(metadata, Error(statusCode, errorType, contentType));
        }

        RemoveAll(metadata, item => item is ProducesErrorMetadata);
    }

    private static void AddIfMissing(IList<object> metadata, ProducesResponseTypeMetadata response)
    {
        bool documented = metadata
            .OfType<IProducesResponseTypeMetadata>()
            .Any(item => item.StatusCode == response.StatusCode);
        if (!documented)
        {
            metadata.Add(response);
        }
    }

    private static void RemoveAll(IList<object> metadata, Func<object, bool> match)
    {
        for (int index = metadata.Count - 1; index >= 0; index--)
        {
            if (match(metadata[index]))
            {
                metadata.RemoveAt(index);
            }
        }
    }
}
