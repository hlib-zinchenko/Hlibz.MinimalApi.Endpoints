using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// Lists every concrete endpoint of the assembly being compiled in a generated
/// <c>EndpointManifestAttribute</c> applied to the assembly, which <c>EndpointDiscovery</c>
/// reads instead of scanning types at runtime. An endpoint it can't list is a build error.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EndpointManifestGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EndpointModel> endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node
                    is ClassDeclarationSyntax { BaseList: not null }
                    or RecordDeclarationSyntax { BaseList: not null },
                static (syntaxContext, cancellationToken) =>
                    EndpointModelBuilder.Build(syntaxContext, cancellationToken))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        IncrementalValueProvider<string?> manifestClassName = context.CompilationProvider
            .Select(static (compilation, _) => ManifestWriter.ClassNameFor(compilation));

        context.RegisterSourceOutput(
            endpoints.Collect().Combine(manifestClassName),
            static (sourceContext, input) => Emit(sourceContext, input.Left, input.Right));
    }

    private static void Emit(
        SourceProductionContext context,
        ImmutableArray<EndpointModel> endpoints,
        string? manifestClassName)
    {
        // A partial endpoint declared with its base list in more than one file is found once per
        // declaration; keep the first.
        List<EndpointModel> distinct = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (EndpointModel endpoint in endpoints.Where(endpoint => seen.Add(endpoint.TypeName)))
        {
            distinct.Add(endpoint);
        }

        foreach (DiagnosticInfo diagnostic in distinct.SelectMany(endpoint => endpoint.Diagnostics))
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        if (manifestClassName is null)
        {
            return;
        }

        context.AddSource(
            "EndpointManifest.g.cs",
            ManifestWriter.Write(
                manifestClassName,
                distinct.Where(endpoint => endpoint.Diagnostics.Count == 0)));
    }
}
