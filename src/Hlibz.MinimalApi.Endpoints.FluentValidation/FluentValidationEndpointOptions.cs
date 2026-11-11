namespace Hlibz.MinimalApi.Endpoints.FluentValidation;

/// <summary>
/// How <see cref="EndpointMappingOptionsExtensions.AddFluentValidation"/> answers and documents a
/// request that fails validation.
/// </summary>
public sealed class FluentValidationEndpointOptions
{
    private Func<ValidationResult, HttpContext, IResult> _onFailure = DefaultFailure;

    /// <summary>
    /// Gets or sets the response to a failed validation. Defaults to a <c>400</c> validation
    /// problem (<c>TypedResults.ValidationProblem</c>) listing the errors by property path.
    /// Setting it also switches the documented body to the mapping's error schema
    /// (<see cref="EndpointMappingOptions.ErrorResponseType"/>).
    /// </summary>
    public Func<ValidationResult, HttpContext, IResult> OnFailure
    {
        get => _onFailure;
        set
        {
            _onFailure = value;
            IsDefaultFailure = false;
        }
    }

    /// <summary>
    /// Gets or sets the status code documented for a failed validation, or
    /// <see langword="null"/> to document none. Defaults to <c>400</c>; set it to match
    /// <see cref="OnFailure"/> when that answers with another status, such as <c>422</c>.
    /// </summary>
    public int? FailureStatusCode { get; set; } = StatusCodes.Status400BadRequest;

    internal bool IsDefaultFailure { get; private set; } = true;

    /// <summary>
    /// Groups a failed validation's error messages by property path, in the order FluentValidation
    /// reported them, the shape <c>TypedResults.ValidationProblem</c> takes.
    /// </summary>
    /// <param name="result">The failed validation.</param>
    /// <returns>The error messages by property path.</returns>
    public static Dictionary<string, string[]> ToErrors(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);
    }

    private static IResult DefaultFailure(ValidationResult result, HttpContext context)
    {
        return TypedResults.ValidationProblem(ToErrors(result));
    }
}
