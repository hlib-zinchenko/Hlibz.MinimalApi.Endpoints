using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// Turns a class declaration into an <see cref="EndpointModel"/> when it is an endpoint the
/// generated list would hold, with the reasons it can't be listed when it is an unusable one.
/// </summary>
internal static class EndpointModelBuilder
{
    private const string EndpointName = "Hlibz.MinimalApi.Endpoints.IEndpoint";
    private const string GroupEndpointName = "Hlibz.MinimalApi.Endpoints.IEndpoint`1";
    private const string ResponseEndpointName = "Hlibz.MinimalApi.Endpoints.IEndpoint`2";

    /// <summary>
    /// Fully qualified names that <c>typeof</c> accepts: <c>global::</c>, keywords for special
    /// types, and tuples written as <c>ValueTuple</c> (element names aren't allowed there).
    /// </summary>
    private static readonly SymbolDisplayFormat TypeOfFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

    /// <summary>
    /// Describes the class declared at <paramref name="context"/>'s node.
    /// </summary>
    /// <param name="context">The class declaration and its semantic model.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <returns>
    /// The endpoint, or <see langword="null"/> when the class isn't a concrete, non-generic
    /// endpoint (or doesn't compile yet).
    /// </returns>
    public static EndpointModel? Build(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        Compilation compilation = context.SemanticModel.Compilation;
        if (context.Node is not TypeDeclarationSyntax declaration
            || context.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken)
                is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } type
            || IsGeneric(type)
            || compilation.GetTypeByMetadataName(EndpointName) is not { } endpointInterface
            || !type.AllInterfaces.Contains(endpointInterface, SymbolEqualityComparer.Default))
        {
            return null;
        }

        ITypeSymbol[] groups = TypeArguments(type, compilation, GroupEndpointName, 0);
        ITypeSymbol[] responses = TypeArguments(type, compilation, ResponseEndpointName, 1);
        if (groups.Concat(responses).Any(argument => argument.TypeKind == TypeKind.Error))
        {
            // The compiler reports that one; a second error for it would only be noise.
            return null;
        }

        Location location = declaration.Identifier.GetLocation();
        string name = type.ToDisplayString();
        List<DiagnosticInfo> diagnostics = [];

        CheckCreatable(type, compilation, location, diagnostics);

        ITypeSymbol? group = groups.Length == 1 ? groups[0] : null;
        if (groups.Length == 0)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoGroup, location, name));
        }
        else if (groups.Length > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.ManyGroups,
                location,
                name,
                string.Join(", ", groups.Select(item => item.ToDisplayString()))));
        }
        else if (group
                 is INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } groupClass)
        {
            CheckCreatable(groupClass, compilation, location, diagnostics);
        }
        else
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.AbstractGroup,
                location,
                groups[0].ToDisplayString()));
        }

        ITypeSymbol? response = responses.Length == 1 ? responses[0] : null;
        if (responses.Length > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.ManyResponseTypes,
                location,
                name,
                string.Join(", ", responses.Select(item => item.ToDisplayString()))));
        }
        else if (response is not null && !IsAccessible(response, compilation))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.Inaccessible,
                location,
                response.ToDisplayString()));
        }

        return new EndpointModel(
            type.ToDisplayString(TypeOfFormat),
            group?.ToDisplayString(TypeOfFormat) ?? string.Empty,
            response?.ToDisplayString(TypeOfFormat),
            SummaryReader.Read(type, cancellationToken),
            new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    /// <summary>
    /// Whether the type or a type it is nested in has type parameters: an open generic type is a
    /// base for endpoints rather than one, as reflection discovery treated it too.
    /// </summary>
    private static bool IsGeneric(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type;
             current is not null;
             current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The distinct type arguments at <paramref name="position"/> of every
    /// <paramref name="definitionName"/> the type implements, directly or through its bases.
    /// </summary>
    private static ITypeSymbol[] TypeArguments(
        INamedTypeSymbol type,
        Compilation compilation,
        string definitionName,
        int position)
    {
        INamedTypeSymbol? definition = compilation.GetTypeByMetadataName(definitionName);
        if (definition is null)
        {
            return [];
        }

        return
        [
            .. type.AllInterfaces
                .Where(item => SymbolEqualityComparer.Default.Equals(
                    item.OriginalDefinition,
                    definition))
                .Select(item => item.TypeArguments[position])
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default),
        ];
    }

    /// <summary>
    /// Reports what stops the generated code from writing <c>new T()</c>.
    /// </summary>
    private static void CheckCreatable(
        INamedTypeSymbol type,
        Compilation compilation,
        Location location,
        List<DiagnosticInfo> diagnostics)
    {
        if (!IsAccessible(type, compilation))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.Inaccessible,
                location,
                type.ToDisplayString()));
            return;
        }

        bool hasConstructor = type.InstanceConstructors.Any(constructor =>
            constructor.Parameters.Length == 0
            && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly));
        if (!hasConstructor)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.NoParameterlessConstructor,
                location,
                type.ToDisplayString()));
        }
    }

    private static bool IsAccessible(ITypeSymbol type, Compilation compilation) =>
        compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
}
