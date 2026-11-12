using System.Reflection;

using Hlibz.MinimalApi.Endpoints.FluentValidation.Internal;

using Microsoft.AspNetCore.Builder;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation;

/// <summary>
/// Adds FluentValidation to <c>MapEndpoints</c>.
/// </summary>
public static class EndpointMappingOptionsExtensions
{
    private const string ValidationProblemContentType = "application/problem+json";

    /// <summary>
    /// Validates every endpoint argument whose type has an <see cref="IValidator{T}"/>
    /// registered, and every such value held by a property of an <c>[AsParameters]</c>
    /// argument: the endpoint gets one filter that validates those values, in parameter order,
    /// before the handler runs and answers with
    /// <see cref="FluentValidationEndpointOptions.OnFailure"/> when it fails, and the failure
    /// status is documented. Which types have a validator is decided once per endpoint, when
    /// routing first builds it (at the first request); the validator itself is resolved from the
    /// request's services, so it may have any lifetime.
    /// </summary>
    /// <param name="options">The mapping options.</param>
    /// <param name="configure">Configures the failure response; the defaults when omitted.</param>
    /// <returns>The options, for chaining.</returns>
    public static EndpointMappingOptions AddFluentValidation(
        this EndpointMappingOptions options,
        Action<FluentValidationEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        FluentValidationEndpointOptions validationOptions = new();
        configure?.Invoke(validationOptions);

        return options.ConfigureEndpoints(context =>
        {
            // The filter: a factory, which ASP.NET Core gives the handler and the app's
            // services when it builds the endpoint; it adds nothing when nothing is validated.
            context.Builder.AddEndpointFilterFactory((factoryContext, next) =>
            {
                ValidationTarget[] targets = ValidationTarget.For(
                    factoryContext.MethodInfo,
                    factoryContext.ApplicationServices);
                if (targets.Length == 0)
                {
                    return next;
                }

                ValidationEndpointFilter filter = new(targets, validationOptions);
                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });

            // The documented failure: metadata, which a filter factory can't add, so a
            // convention reaches the same decision on the same handler.
            context.Builder.Add(endpointBuilder => Document(endpointBuilder, validationOptions));
        });
    }

    private static void Document(EndpointBuilder endpoint, FluentValidationEndpointOptions options)
    {
        if (options.FailureStatusCode is not { } statusCode
            || endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault() is not { } handler
            || ValidationTarget.For(handler, endpoint.ApplicationServices).Length == 0)
        {
            return;
        }

        endpoint.Metadata.Add(options.IsDefaultFailure
            ? new ProducesResponseTypeMetadata(
                statusCode,
                typeof(HttpValidationProblemDetails),
                [ValidationProblemContentType])
            : new ProducesErrorMetadata(statusCode));
    }
}
