using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Hlibz.MinimalApi.Endpoints.Generators.Tests;

/// <summary>
/// One source compiled in memory with <see cref="EndpointMapAnalyzer"/>: what it reported, and
/// the compiler's own errors.
/// </summary>
internal sealed class AnalyzerRun
{
    private AnalyzerRun(Compilation compilation, ImmutableArray<Diagnostic> diagnostics)
    {
        Compilation = compilation;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the compilation the analyzer ran on.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Gets what the analyzer reported.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the ids of what the analyzer reported.
    /// </summary>
    public string[] DiagnosticIds => [.. Diagnostics.Select(item => item.Id)];

    public static async Task<AnalyzerRun> OfAsync(string source)
    {
        CSharpCompilation compilation = GeneratorRun.Compile([source]);
        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers(
                [new EndpointMapAnalyzer()],
                new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions([]),
                    onAnalyzerException: null,
                    concurrentAnalysis: false,
                    logAnalyzerExecutionTime: false,
                    reportSuppressedDiagnostics: false))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        return new AnalyzerRun(compilation, diagnostics);
    }

    /// <summary>
    /// Gets the source text a diagnostic points at.
    /// </summary>
    /// <param name="diagnostic">A diagnostic reported on the source.</param>
    /// <returns>The text it underlines.</returns>
    public string SourceAt(Diagnostic diagnostic)
    {
        SyntaxTree tree = Compilation.SyntaxTrees.Single(
            item => item == diagnostic.Location.SourceTree);
        return tree
            .GetText(TestContext.Current.CancellationToken)
            .ToString(diagnostic.Location.SourceSpan);
    }

    /// <summary>
    /// Gets the compiler's errors, so a test can't pass on a source that doesn't compile.
    /// </summary>
    /// <returns>The errors.</returns>
    public Diagnostic[] CompileErrors() =>
    [
        .. Compilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error),
    ];
}
