namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Internal;

/// <summary>
/// Validates an endpoint's <see cref="ValidationTarget"/>s, in parameter order, before the
/// handler runs, and answers the first failure with
/// <see cref="FluentValidationEndpointOptions.OnFailure"/>.
/// </summary>
internal sealed class ValidationEndpointFilter : IEndpointFilter
{
    private readonly ValidationTarget[] _targets;

    private readonly FluentValidationEndpointOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationEndpointFilter"/> class.
    /// </summary>
    /// <param name="targets">The values to validate.</param>
    /// <param name="options">How a failure is answered.</param>
    public ValidationEndpointFilter(
        ValidationTarget[] targets,
        FluentValidationEndpointOptions options)
    {
        _targets = targets;
        _options = options;
    }

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        foreach (ValidationTarget target in _targets)
        {
            if (target.ValueIn(context.Arguments) is not { } value)
            {
                continue;
            }

            ValidationResult result = await target.ValidateAsync(value, context.HttpContext);
            if (!result.IsValid)
            {
                return _options.OnFailure(result, context.HttpContext);
            }
        }

        return await next(context);
    }
}
