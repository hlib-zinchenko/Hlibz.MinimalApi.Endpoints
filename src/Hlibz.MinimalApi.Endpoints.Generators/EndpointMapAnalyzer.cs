using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// Checks each endpoint's <c>IEndpoint.Map</c> body: it maps exactly one route on its group
/// parameter (<c>HMAE007</c>). That's one call taking the group as its first argument and
/// returning a <c>RouteHandlerBuilder</c>: <c>MapGet</c> and the like, or an existing extension
/// that maps one route (<c>MapErrorCodes()</c>, say), which the analyzer trusts to map just one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointMapAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.MapsNotOneRoute);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            if (KnownSymbols.Resolve(compilationContext.Compilation) is { } symbols)
            {
                compilationContext.RegisterOperationBlockAction(
                    blockContext => AnalyzeBlock(blockContext, symbols));
            }
        });
    }

    private static void AnalyzeBlock(OperationBlockAnalysisContext context, KnownSymbols symbols)
    {
        if (context.OwningSymbol is not IMethodSymbol { Parameters.Length: 1 } method
            || !IsMapImplementation(method, symbols))
        {
            return;
        }

        IParameterSymbol group = method.Parameters[0];
        IParameterReferenceOperation[] references =
        [
            .. context.OperationBlocks
                .SelectMany(block => block.DescendantsAndSelf())
                .OfType<IParameterReferenceOperation>()
                .Where(reference => SymbolEqualityComparer.Default.Equals(
                    reference.Parameter,
                    group)),
        ];

        IInvocationOperation? route = references.Length == 1
            ? RouteCallOf(references[0], symbols)
            : null;
        if (route is null || IsNestedInLoopOrFunction(route))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MapsNotOneRoute,
                method.Locations.FirstOrDefault(),
                method.ContainingType.ToDisplayString()));
        }
    }

    /// <summary>
    /// Whether <paramref name="method"/> is its type's implementation of <c>IEndpoint.Map</c>,
    /// explicit or not, declared there or overriding a base class's.
    /// </summary>
    private static bool IsMapImplementation(IMethodSymbol method, KnownSymbols symbols)
    {
        // Roslyn names the base class's member, not the override, as the implementation.
        if (method.ContainingType?.FindImplementationForInterfaceMember(symbols.Map)
            is not IMethodSymbol implementation)
        {
            return false;
        }

        for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
        {
            if (SymbolEqualityComparer.Default.Equals(
                    current.OriginalDefinition,
                    implementation.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The call the group reference is the first argument of, when it returns a
    /// <c>RouteHandlerBuilder</c> (one route's): <c>MapGet</c>/<c>MapPost</c>/..., or any other
    /// method mapping one route. <see langword="null"/> when the group is used any other way.
    /// </summary>
    private static IInvocationOperation? RouteCallOf(
        IParameterReferenceOperation reference,
        KnownSymbols symbols)
    {
        IOperation operation = reference;
        while (operation.Parent is IConversionOperation conversion)
        {
            operation = conversion;
        }

        return operation.Parent
                   is IArgumentOperation { Parent: IInvocationOperation invocation } argument
               && argument.Parameter?.Ordinal == 0
               && SymbolEqualityComparer.Default.Equals(
                   invocation.TargetMethod.ReturnType,
                   symbols.RouteHandlerBuilder)
            ? invocation
            : null;
    }

    private static bool IsNestedInLoopOrFunction(IOperation operation)
    {
        for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
        {
            if (current is ILoopOperation
                or IAnonymousFunctionOperation
                or ILocalFunctionOperation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The types and members the analysis compares against, resolved once per compilation.
    /// </summary>
    private sealed class KnownSymbols
    {
        private KnownSymbols(IMethodSymbol map, INamedTypeSymbol routeHandlerBuilder)
        {
            Map = map;
            RouteHandlerBuilder = routeHandlerBuilder;
        }

        public IMethodSymbol Map { get; }

        public INamedTypeSymbol RouteHandlerBuilder { get; }

        /// <summary>
        /// Resolves the symbols, or returns <see langword="null"/> when the compilation doesn't
        /// reference the library (or ASP.NET Core) and there is nothing to check.
        /// </summary>
        public static KnownSymbols? Resolve(Compilation compilation)
        {
            if (compilation.GetTypeByMetadataName("Hlibz.MinimalApi.Endpoints.IEndpoint")
                    ?.GetMembers("Map").OfType<IMethodSymbol>().FirstOrDefault() is not { } map
                || compilation.GetTypeByMetadataName(
                    "Microsoft.AspNetCore.Builder.RouteHandlerBuilder") is not { } builder)
            {
                return null;
            }

            return new KnownSymbols(map, builder);
        }
    }
}
