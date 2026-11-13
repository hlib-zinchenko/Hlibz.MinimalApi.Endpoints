using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// The build errors and warnings: endpoints the generator can't list (<c>HMAE001</c>-<c>HMAE006</c>,
/// each a startup failure when discovery used reflection, and <c>HMAE010</c>/<c>HMAE011</c>,
/// once silently left out), and <c>Map</c> bodies the analyzer rejects (<c>HMAE007</c>).
/// <c>HMAE003</c>, <c>HMAE008</c> and <c>HMAE009</c> are retired: they checked the endpoint's
/// response type, which endpoints no longer declare.
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
    /// HMAE010: the endpoint is a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor StructEndpoint = Error(
        "HMAE010",
        "Endpoint is a struct",
        "{0} is a struct; an endpoint must be a class");

    /// <summary>
    /// HMAE011: the endpoint is generic, or nested in a generic type, so it is never mapped. A
    /// warning: a generic class can be a base for endpoints, which <c>abstract</c> says.
    /// </summary>
    public static readonly DiagnosticDescriptor GenericEndpoint = Warning(
        "HMAE011",
        "Generic endpoint is never mapped",
        Messages.GenericEndpointText);

    private static DiagnosticDescriptor Error(string id, string title, string message) =>
        Descriptor(id, title, message, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor Warning(string id, string title, string message) =>
        Descriptor(id, title, message, DiagnosticSeverity.Warning);

    private static DiagnosticDescriptor Descriptor(
        string id,
        string title,
        string message,
        DiagnosticSeverity severity) =>
        new(
            id,
            title,
            message,
            Messages.Category,
            severity,
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
            "{0} implements IEndpoint directly; implement IEndpoint<TGroup> "
            + "instead";

        public const string NoParameterlessConstructorText =
            "{0} has no public or internal parameterless constructor; endpoints and endpoint "
            + "groups are created once at startup, so their dependencies belong in the handler's "
            + "parameters";

        public const string MapsNotOneRouteText =
            "{0}.Map must map exactly one route: one call on its group parameter that returns a "
            + "RouteHandlerBuilder (MapGet, MapPost, MapPut, MapDelete, MapPatch, MapMethods, or an "
            + "extension that maps one route), outside loops and lambdas, using the parameter "
            + "nowhere else";

        public const string GenericEndpointText =
            "{0} is generic or nested in a generic type, so it is never mapped; make it abstract "
            + "if it is a base for endpoints, and derive non-generic endpoints from it";

        public const string InaccessibleText =
            "{0} isn't accessible to the generated endpoint list; make it, and any type it is "
            + "nested in, internal or public";
    }
}
