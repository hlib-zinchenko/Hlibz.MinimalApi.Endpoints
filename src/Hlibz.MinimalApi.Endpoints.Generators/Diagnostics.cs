using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// The build errors for endpoints the generator can't list. Each was a startup failure when
/// discovery used reflection; the build catches it first now.
/// </summary>
internal static class Diagnostics
{
    /// <summary>
    /// HMAE001: the endpoint implements <c>IEndpoint</c> without naming a group.
    /// </summary>
    public static readonly DiagnosticDescriptor NoGroup = Error(
        "HMAE001",
        "Endpoint has no group",
        Messages.NoGroupText);

    /// <summary>
    /// HMAE002: the endpoint names more than one group.
    /// </summary>
    public static readonly DiagnosticDescriptor ManyGroups = Error(
        "HMAE002",
        "Endpoint names more than one group",
        "{0} names more than one endpoint group: {1}");

    /// <summary>
    /// HMAE003: the endpoint names more than one response type.
    /// </summary>
    public static readonly DiagnosticDescriptor ManyResponseTypes = Error(
        "HMAE003",
        "Endpoint names more than one response type",
        "{0} names more than one response type: {1}");

    /// <summary>
    /// HMAE004: the endpoint or its group has no parameterless constructor the generated code can
    /// call.
    /// </summary>
    public static readonly DiagnosticDescriptor NoParameterlessConstructor = Error(
        "HMAE004",
        "No parameterless constructor",
        Messages.NoParameterlessConstructorText);

    /// <summary>
    /// HMAE005: the group the endpoint names is abstract or an interface.
    /// </summary>
    public static readonly DiagnosticDescriptor AbstractGroup = Error(
        "HMAE005",
        "Endpoint group is abstract",
        "{0} is abstract; the group an endpoint names must be a concrete class");

    /// <summary>
    /// HMAE006: a type the generated code has to name isn't accessible to it.
    /// </summary>
    public static readonly DiagnosticDescriptor Inaccessible = Error(
        "HMAE006",
        "Type isn't accessible to the generated endpoint list",
        Messages.InaccessibleText);

    private static DiagnosticDescriptor Error(string id, string title, string message) =>
        new(
            id,
            title,
            message,
            Messages.Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: Messages.HelpLink);

    /// <summary>
    /// The longer texts, kept apart so each descriptor reads on a few lines.
    /// </summary>
    private static class Messages
    {
        public const string Category = "Hlibz.MinimalApi.Endpoints";

        public const string HelpLink =
            "https://github.com/hlib-zinchenko/Hlibz.MinimalApi.Endpoints#build-errors";

        public const string NoGroupText =
            "{0} implements IEndpoint directly; implement IEndpoint<TGroup> or "
            + "IEndpoint<TGroup, TResponse> instead";

        public const string NoParameterlessConstructorText =
            "{0} has no public or internal parameterless constructor; endpoints and endpoint "
            + "groups are created once at startup, so their dependencies belong in the handler's "
            + "parameters";

        public const string InaccessibleText =
            "{0} isn't accessible to the generated endpoint list; make it, and any type it is "
            + "nested in, internal or public";
    }
}
