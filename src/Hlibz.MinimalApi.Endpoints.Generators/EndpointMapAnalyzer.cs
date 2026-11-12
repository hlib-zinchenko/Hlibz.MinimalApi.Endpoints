using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Hlibz.MinimalApi.Endpoints.Generators;

/// <summary>
/// Checks each endpoint's <c>IEndpoint.Map</c> body: it maps exactly one route on its group
/// parameter (<c>HMAE007</c>), and the handler of an <c>IEndpoint&lt;TGroup, TResponse&gt;</c>
/// returns <c>TResponse</c> or a typed result carrying it (<c>HMAE008</c>, <c>HMAE009</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointMapAnalyzer : DiagnosticAnalyzer
{
    private const string HandlerParameterName = "handler";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Diagnostics.MapsNotOneRoute,
            Diagnostics.HandlerResponseMismatch,
            Diagnostics.HandlerResponseUnchecked);

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
            return;
        }

        CheckHandler(context, method.ContainingType, route, symbols);
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
    /// The <c>MapGet</c>/<c>MapPost</c>/... call the group reference is the first argument of,
    /// or <see langword="null"/> when the group is used any other way.
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
                   invocation.TargetMethod.ContainingType,
                   symbols.RouteExtensions)
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

    private static void CheckHandler(
        OperationBlockAnalysisContext context,
        INamedTypeSymbol endpointType,
        IInvocationOperation route,
        KnownSymbols symbols)
    {
        ITypeSymbol[] responses =
        [
            .. endpointType.AllInterfaces
                .Where(item => SymbolEqualityComparer.Default.Equals(
                    item.OriginalDefinition,
                    symbols.ResponseEndpoint))
                .Select(item => item.TypeArguments[1])
                .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default),
        ];
        if (responses.Length != 1 || responses[0].TypeKind == TypeKind.Error)
        {
            // No TResponse to check against, or HMAE003 already covers it.
            return;
        }

        ITypeSymbol response = responses[0];

        IArgumentOperation? handler = route.Arguments.FirstOrDefault(
            argument => argument.Parameter?.Name == HandlerParameterName);
        if (handler is null)
        {
            return;
        }

        string endpoint = endpointType.ToDisplayString();
        string responseName = response.ToDisplayString();
        if (ReturnTypeOf(handler.Value) is not { } returnType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.HandlerResponseUnchecked,
                handler.Syntax.GetLocation(),
                endpoint,
                responseName,
                "isn't a method group or lambda"));
            return;
        }

        ITypeSymbol result = UnwrapTask(returnType, symbols);
        if (result.TypeKind == TypeKind.Error || Carries(result, response, symbols))
        {
            return;
        }

        if (result.SpecialType == SpecialType.System_Object
            || result.TypeKind == TypeKind.Dynamic
            || SymbolEqualityComparer.Default.Equals(result, symbols.Result))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.HandlerResponseUnchecked,
                handler.Syntax.GetLocation(),
                endpoint,
                responseName,
                "returns " + returnType.ToDisplayString()));
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.HandlerResponseMismatch,
            handler.Syntax.GetLocation(),
            endpoint,
            returnType.ToDisplayString(),
            responseName));
    }

    /// <summary>
    /// The return type of a handler given as a method group or a lambda, or
    /// <see langword="null"/> for any other delegate (a field, property, local or call).
    /// </summary>
    private static ITypeSymbol? ReturnTypeOf(IOperation value)
    {
        IOperation operation = value;
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversion:
                    operation = conversion.Operand;
                    break;
                case IDelegateCreationOperation creation:
                    operation = creation.Target;
                    break;
                case IMethodReferenceOperation reference:
                    return reference.Method.ReturnType;
                case IAnonymousFunctionOperation lambda:
                    return lambda.Symbol.ReturnType;
                default:
                    return null;
            }
        }
    }

    private static ITypeSymbol UnwrapTask(ITypeSymbol type, KnownSymbols symbols) =>
        type is INamedTypeSymbol { TypeArguments.Length: 1 } named
        && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.Task)
            || SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, symbols.ValueTask))
            ? named.TypeArguments[0]
            : type;

    /// <summary>
    /// Whether <paramref name="type"/> is <paramref name="response"/>, a value result of it
    /// (<c>Ok&lt;T&gt;</c>, <c>Created&lt;T&gt;</c>, ...), or a <c>Results&lt;...&gt;</c> union
    /// with an arm that carries it.
    /// </summary>
    private static bool Carries(ITypeSymbol type, ITypeSymbol response, KnownSymbols symbols)
    {
        if (SymbolEqualityComparer.Default.Equals(type, response))
        {
            return true;
        }

        if (symbols.ValueResult is { } valueResult)
        {
            IEnumerable<INamedTypeSymbol> candidates = type is INamedTypeSymbol named
                ? type.AllInterfaces.Prepend(named)
                : type.AllInterfaces;
            if (candidates.Any(item => IsValueResultOf(item, valueResult, response)))
            {
                return true;
            }
        }

        return symbols.NestedResult is { } nestedResult
               && type is INamedTypeSymbol { TypeArguments.Length: > 0 } union
               && union.AllInterfaces.Contains(nestedResult, SymbolEqualityComparer.Default)
               && union.TypeArguments.Any(argument => Carries(argument, response, symbols));
    }

    private static bool IsValueResultOf(
        INamedTypeSymbol type,
        INamedTypeSymbol valueResult,
        ITypeSymbol response) =>
        SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, valueResult)
        && SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], response);

    /// <summary>
    /// The types and members the analysis compares against, resolved once per compilation.
    /// </summary>
    private sealed class KnownSymbols
    {
        private KnownSymbols(
            IMethodSymbol map,
            INamedTypeSymbol? responseEndpoint,
            INamedTypeSymbol routeHandlerBuilder,
            INamedTypeSymbol routeExtensions,
            Compilation compilation)
        {
            Map = map;
            ResponseEndpoint = responseEndpoint;
            RouteHandlerBuilder = routeHandlerBuilder;
            RouteExtensions = routeExtensions;
            ValueResult = compilation.GetTypeByMetadataName(
                "Microsoft.AspNetCore.Http.IValueHttpResult`1");
            NestedResult = compilation.GetTypeByMetadataName(
                "Microsoft.AspNetCore.Http.INestedHttpResult");
            Result = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IResult");
            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        }

        public IMethodSymbol Map { get; }

        public INamedTypeSymbol? ResponseEndpoint { get; }

        public INamedTypeSymbol RouteHandlerBuilder { get; }

        public INamedTypeSymbol RouteExtensions { get; }

        public INamedTypeSymbol? ValueResult { get; }

        public INamedTypeSymbol? NestedResult { get; }

        public INamedTypeSymbol? Result { get; }

        public INamedTypeSymbol? Task { get; }

        public INamedTypeSymbol? ValueTask { get; }

        /// <summary>
        /// Resolves the symbols, or returns <see langword="null"/> when the compilation doesn't
        /// reference the library (or ASP.NET Core) and there is nothing to check.
        /// </summary>
        public static KnownSymbols? Resolve(Compilation compilation)
        {
            if (compilation.GetTypeByMetadataName("Hlibz.MinimalApi.Endpoints.IEndpoint")
                    ?.GetMembers("Map").OfType<IMethodSymbol>().FirstOrDefault() is not { } map
                || compilation.GetTypeByMetadataName(
                    "Microsoft.AspNetCore.Builder.RouteHandlerBuilder") is not { } builder
                || compilation.GetTypeByMetadataName(
                    "Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions")
                    is not { } extensions)
            {
                return null;
            }

            return new KnownSymbols(
                map,
                compilation.GetTypeByMetadataName("Hlibz.MinimalApi.Endpoints.IEndpoint`2"),
                builder,
                extensions,
                compilation);
        }
    }
}
