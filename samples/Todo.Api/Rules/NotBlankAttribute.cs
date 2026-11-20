using Hlibz.OpenApi;

using Microsoft.OpenApi;

namespace Todo.Api.Rules;

/// <summary>
/// Documents that a text must not be blank. The check itself is the store's, which answers a
/// blank text with <c>Result.Invalid</c>: a <c>400</c> with the field in
/// <c>validationErrors</c>. This attribute only tells API readers, through Hlibz.OpenApi's
/// <c>AddOpenApiRules()</c>, as a <c>minLength</c> and <c>pattern</c> plus a sentence.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotBlankAttribute : Attribute, IOpenApiRule
{
    public void Describe(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        schema.MinLength = 1;
        schema.Pattern = @"\S";
        RuleText.Add(schema, "Must not be blank; a blank one is a `400` naming it in `validationErrors`.");
    }
}
