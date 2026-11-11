namespace Hlibz.MinimalApi.Endpoints.FluentValidation.Internal;

/// <summary>
/// Validates every argument of type <typeparamref name="T"/> before the handler runs.
/// </summary>
/// <typeparam name="T">The validated argument type.</typeparam>
internal sealed class ValidationEndpointFilter<T> : IEndpointFilter
{
    private readonly FluentValidationEndpointOptions _options;

    public ValidationEndpointFilter(FluentValidationEndpointOptions options)
    {
        _options = options;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        IValidator<T>? validator = null;

        foreach (object? argument in context.Arguments)
        {
            if (argument is not T validatable)
            {
                continue;
            }

            validator ??= context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();

            ValidationResult result = await validator.ValidateAsync(
                validatable,
                context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                return _options.OnFailure(result, context.HttpContext);
            }
        }

        return await next(context);
    }
}
