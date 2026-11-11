using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;

namespace Hlibz.MinimalApi.Endpoints.Internal;

/// <summary>
/// Builds the documented responses: errors with the configured schema, and the authorization and
/// rate limiting gates' responses read off the endpoint's final metadata.
/// </summary>
internal static class ResponseDocumentation
{
    public static ProducesResponseTypeMetadata Error(int statusCode, EndpointMappingOptions options)
    {
        return options.ErrorResponseType is null
            ? new ProducesResponseTypeMetadata(statusCode)
            : new ProducesResponseTypeMetadata(
                statusCode,
                options.ErrorResponseType,
                [options.ErrorContentType]);
    }

    /// <summary>
    /// Documents <c>401</c>/<c>403</c> and <c>429</c> once the endpoint's metadata is final, so
    /// authorization and rate limiting count wherever they came from: the endpoint, its group, a
    /// convention, or an outer route group the app mapped the endpoints into.
    /// </summary>
    /// <param name="builder">The endpoint's route handler builder.</param>
    /// <param name="options">The mapping options.</param>
    public static void DocumentGateResponses(
        RouteHandlerBuilder builder,
        EndpointMappingOptions options)
    {
        // Captured now: the options aren't read again once mapping is over.
        bool documentAuthorization = options.DocumentAuthorizationResponses;
        bool documentRateLimit = options.DocumentRateLimitResponses;
        ProducesResponseTypeMetadata unauthorized =
            Error(StatusCodes.Status401Unauthorized, options);
        ProducesResponseTypeMetadata forbidden = Error(StatusCodes.Status403Forbidden, options);
        ProducesResponseTypeMetadata tooManyRequests =
            Error(StatusCodes.Status429TooManyRequests, options);

        builder.Finally(endpoint =>
        {
            IList<object> metadata = endpoint.Metadata;

            if (documentAuthorization && RequiresAuthorization(metadata))
            {
                AddIfMissing(metadata, unauthorized);
                AddIfMissing(metadata, forbidden);
            }

            if (documentRateLimit && IsRateLimited(metadata))
            {
                AddIfMissing(metadata, tooManyRequests);
            }
        });
    }

    internal static bool RequiresAuthorization(IList<object> metadata) =>
        metadata.Any(item => item is IAuthorizeData or AuthorizationPolicy)
        && !metadata.Any(item => item is IAllowAnonymous);

    internal static bool IsRateLimited(IList<object> metadata) =>
        metadata.Any(item => item is EnableRateLimitingAttribute)
        && !metadata.Any(item => item is DisableRateLimitingAttribute);

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
}
