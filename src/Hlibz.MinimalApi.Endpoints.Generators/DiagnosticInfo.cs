using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// A diagnostic to report, kept as plain values rather than a <see cref="Diagnostic"/> (which
/// holds on to the syntax tree) so the incremental pipeline can cache it.
/// </summary>
/// <param name="Descriptor">What is reported.</param>
/// <param name="FilePath">The file it is reported in.</param>
/// <param name="TextSpan">Where in the file.</param>
/// <param name="LineSpan">The same place as lines and columns.</param>
/// <param name="Arguments">The message's arguments.</param>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan,
    EquatableArray<string> Arguments)
{
    /// <summary>
    /// Creates a diagnostic to report at <paramref name="location"/>.
    /// </summary>
    /// <param name="descriptor">What is reported.</param>
    /// <param name="location">Where.</param>
    /// <param name="arguments">The message's arguments.</param>
    /// <returns>The diagnostic.</returns>
    public static DiagnosticInfo Create(
        DiagnosticDescriptor descriptor,
        Location location,
        params string[] arguments)
    {
        return new DiagnosticInfo(
            descriptor,
            location.SourceTree?.FilePath ?? string.Empty,
            location.SourceSpan,
            location.GetLineSpan().Span,
            new EquatableArray<string>(arguments));
    }

    /// <summary>
    /// Creates the <see cref="Diagnostic"/> to report.
    /// </summary>
    /// <returns>The diagnostic.</returns>
    public Diagnostic ToDiagnostic()
    {
        return Diagnostic.Create(
            Descriptor,
            Location.Create(FilePath, TextSpan, LineSpan),
            [.. Arguments]);
    }
}
