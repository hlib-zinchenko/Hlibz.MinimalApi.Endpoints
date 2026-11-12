using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// The build errors: endpoints the generator can't list (<c>HMAE001</c>-<c>HMAE006</c>, each a
/// startup failure when discovery used reflection), and <c>Map</c> bodies the analyzer rejects
/// (<c>HMAE007</c>-<c>HMAE009</c>).
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

    /// <summary>
    /// HMAE007: the endpoint's <c>Map</c> doesn't map exactly one route on its group.
    /// </summary>
    public static readonly DiagnosticDescriptor MapsNotOneRoute = Error(
        "HMAE007",
        "Map must map exactly one route",
        Messages.MapsNotOneRouteText);

    /// <summary>
    /// HMAE008: the handler of an endpoint with a response type returns something that doesn't
    /// carry it.
    /// </summary>
    public static readonly DiagnosticDescriptor HandlerResponseMismatch = Error(
        "HMAE008",
        "Handler doesn't return the endpoint's response type",
        Messages.HandlerResponseMismatchText);

    /// <summary>
    /// HMAE009: the handler of an endpoint with a response type returns something the build can't
    /// check against it.
    /// </summary>
    public static readonly DiagnosticDescriptor HandlerResponseUnchecked = Error(
        "HMAE009",
        "Handler's response can't be checked",
        Messages.HandlerResponseUncheckedText);

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

        public const string MapsNotOneRouteText =
            "{0}.Map must map exactly one route: one MapGet, MapPost, MapPut, MapDelete, MapPatch "
            + "or MapMethods call on its group parameter, outside loops and lambdas, using the "
            + "parameter nowhere else";

        public const string HandlerResponseMismatchText =
            "{0}'s handler returns {1}, which doesn't carry the endpoint's response type {2}; "
            + "return {2} or a typed result carrying it, such as Ok<{2}>, or implement "
            + "IEndpoint<TGroup> instead";

        public const string HandlerResponseUncheckedText =
            "{0} declares the response type {1}, but its handler {2}, so the build can't check "
            + "it; return a typed result such as Ok<{1}> or Results<Ok<{1}>, NotFound>, or "
            + "implement IEndpoint<TGroup> instead";

        public const string InaccessibleText =
            "{0} isn't accessible to the generated endpoint list; make it, and any type it is "
            + "nested in, internal or public";
    }
}
