using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hlibz.MinimalApi.Endpoints.Generators.Tests;

/// <summary>
/// One source compiled in memory with the generator: what it reported, what it wrote, and the
/// assembly it all compiles to.
/// </summary>
internal sealed class GeneratorRun
{
    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using System.Net.Http;
        using System.Threading.Tasks;
        using Hlibz.MinimalApi.Endpoints;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Http;
        using Microsoft.AspNetCore.Http.HttpResults;
        using Microsoft.AspNetCore.Routing;

        """;

    private static readonly MetadataReference[] AllReferences =
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path)),
    ];

    private GeneratorRun(
        ImmutableArray<Diagnostic> generatorDiagnostics,
        Compilation output,
        string? manifest)
    {
        GeneratorDiagnostics = generatorDiagnostics;
        Output = output;
        Manifest = manifest;
    }

    /// <summary>
    /// Gets what the generator reported.
    /// </summary>
    public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

    /// <summary>
    /// Gets the compilation with the generated source added.
    /// </summary>
    public Compilation Output { get; }

    /// <summary>
    /// Gets the generated manifest source, or null when nothing was generated.
    /// </summary>
    public string? Manifest { get; }

    /// <summary>
    /// Gets the ids of what the generator reported.
    /// </summary>
    public string[] DiagnosticIds => [.. GeneratorDiagnostics.Select(item => item.Id)];

    public static GeneratorRun Of(string source, bool referenceLibrary = true) =>
        Of([source], referenceLibrary);

    public static GeneratorRun Of(
        string[] sources,
        bool referenceLibrary = true,
        DocumentationMode documentationMode = DocumentationMode.Parse)
    {
        CSharpCompilation compilation = Compile(sources, referenceLibrary, documentationMode);

        GeneratorDriver driver = CSharpGeneratorDriver
            .Create(new EndpointManifestGenerator())
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out Compilation output,
                out ImmutableArray<Diagnostic> diagnostics);

        string? manifest = driver.GetRunResult().GeneratedTrees
            .Select(tree => tree.ToString())
            .SingleOrDefault();
        return new GeneratorRun(diagnostics, output, manifest);
    }

    /// <summary>
    /// Compiles <paramref name="sources"/> in memory, each after the shared usings, against the
    /// test runtime's assemblies.
    /// </summary>
    /// <param name="sources">The sources.</param>
    /// <param name="referenceLibrary">Whether to reference Hlibz.MinimalApi.Endpoints.</param>
    /// <param name="documentationMode">
    /// How the compiler reads documentation comments; <see cref="DocumentationMode.None"/> is a
    /// project without <c>GenerateDocumentationFile</c>.
    /// </param>
    /// <returns>The compilation, generator not run.</returns>
    public static CSharpCompilation Compile(
        string[] sources,
        bool referenceLibrary = true,
        DocumentationMode documentationMode = DocumentationMode.Parse)
    {
        CSharpParseOptions parseOptions = new(LanguageVersion.CSharp12, documentationMode);
        MetadataReference[] references = referenceLibrary
            ? AllReferences
            : [.. AllReferences.Where(reference => !reference.Display!.EndsWith(
                "Hlibz.MinimalApi.Endpoints.dll",
                StringComparison.Ordinal))];
        return CSharpCompilation.Create(
            "Generated.Endpoints-1",
            sources.Select((source, index) => CSharpSyntaxTree.ParseText(
                Usings + source,
                parseOptions,
                path: $"Source{index}.cs")),
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>
    /// Emits the output and loads it into a context of its own, failing on any compile error.
    /// </summary>
    /// <returns>The loaded assembly.</returns>
    public Assembly Load()
    {
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = Output.Emit(stream);
        Assert.True(
            result.Success,
            string.Join(Environment.NewLine, result.Diagnostics.Where(IsError)));

        stream.Position = 0;
        return new AssemblyLoadContext(name: null, isCollectible: true).LoadFromStream(stream);
    }

    /// <summary>
    /// Gets the source text a diagnostic points at.
    /// </summary>
    /// <param name="diagnostic">A diagnostic reported on one of the sources.</param>
    /// <returns>The text it underlines.</returns>
    public string SourceAt(Diagnostic diagnostic)
    {
        string path = diagnostic.Location.GetLineSpan().Path;
        SyntaxTree tree = Output.SyntaxTrees.Single(item => item.FilePath == path);
        return tree.GetText(TestContext.Current.CancellationToken)
            .ToString(diagnostic.Location.SourceSpan);
    }

    /// <summary>
    /// Gets the output's compile errors (the generated source's included).
    /// </summary>
    /// <returns>The errors.</returns>
    public Diagnostic[] CompileErrors() => [.. Output.GetDiagnostics().Where(IsError)];

    private static bool IsError(Diagnostic diagnostic) =>
        diagnostic.Severity == DiagnosticSeverity.Error;
}
