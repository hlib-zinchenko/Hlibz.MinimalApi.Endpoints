namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// One endpoint as the generator sees it: plain strings, so the incremental pipeline can tell
/// when nothing changed.
/// </summary>
/// <param name="TypeName">The endpoint type, fully qualified with <c>global::</c>.</param>
/// <param name="GroupTypeName">
/// The group type, fully qualified; empty when it can't be listed.
/// </param>
/// <param name="ResponseTypeName">The success response type, fully qualified, or null.</param>
/// <param name="Diagnostics">Why the endpoint can't be listed; empty when it can.</param>
internal sealed record EndpointModel(
    string TypeName,
    string GroupTypeName,
    string? ResponseTypeName,
    EquatableArray<DiagnosticInfo> Diagnostics);
