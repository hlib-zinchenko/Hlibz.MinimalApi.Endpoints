using Hlibz.MinimalApi.Endpoints.FluentValidation.Internal;

using Microsoft.AspNetCore.Builder;

namespace Hlibz.MinimalApi.Endpoints.FluentValidation;

/// <summary>
/// Adds FluentValidation to <c>MapEndpoints</c>.
/// </summary>
public static class EndpointMappingOptionsExtensions
{
    /// <summary>
    /// Validates every endpoint argument whose type has an <see cref="IValidator{T}"/>
    /// registered: for each such handler parameter type the endpoint gets a filter that validates
    /// the argument before the handler runs and answers with
    /// <see cref="FluentValidationEndpointOptions.OnFailure"/> when it fails, and the failure
    /// status is documented. Which types have a validator is decided once, at startup; the
    /// validator itself is resolved from the request's services, so it may have any lifetime.
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

        return options.ConfigureEndpoints(context => Apply(context, validationOptions));
    }

    private static void Apply(EndpointContext context, FluentValidationEndpointOptions options)
    {
        IServiceProviderIsService isService =
            context.Services.GetRequiredService<IServiceProviderIsService>();

        Type[] validatedTypes =
        [
            .. context.Endpoint.Handler.Method
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .Where(CanBeValidated)
                .Distinct()
                .Where(type => isService.IsService(typeof(IValidator<>).MakeGenericType(type))),
        ];

        if (validatedTypes.Length == 0)
        {
            return;
        }

        foreach (Type type in validatedTypes)
        {
            IEndpointFilter filter = (IEndpointFilter)Activator.CreateInstance(
                typeof(ValidationEndpointFilter<>).MakeGenericType(type),
                options)!;
            context.Builder.AddEndpointFilter(filter);
        }

        if (options.FailureStatusCode is not { } statusCode)
        {
            return;
        }

        if (options.IsDefaultFailure)
        {
            context.Builder.ProducesValidationProblem(statusCode);
        }
        else
        {
            context.ProducesError(statusCode);
        }
    }

    /// <summary>
    /// Whether <paramref name="type"/> can be a generic argument at all: by-ref, pointer and
    /// ref struct parameters (<c>ref</c>/<c>out</c>, <c>Span&lt;T&gt;</c>) can't.
    /// </summary>
    private static bool CanBeValidated(Type type) =>
        type is
        {
            IsByRef: false,
            IsPointer: false,
            IsByRefLike: false,
            ContainsGenericParameters: false,
        };
}
