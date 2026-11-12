using System.Globalization;

using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators.Tests;

public sealed class EndpointMapAnalyzerTests
{
    private const string Groups = """
        namespace App;

        internal sealed class BooksGroup : IEndpointGroup
        {
            public string Prefix => "api/books";
        }

        internal sealed record Book(int Id);

        """;

    [Theory]
    [InlineData("""
        {
            group.MapGet("a", () => Results.Ok());
            return group.MapGet("b", () => Results.Ok());
        }
        """)]
    [InlineData("=> null!;")]
    [InlineData("""
        {
            RouteHandlerBuilder builder = null!;
            foreach (string route in new[] { "a", "b" })
            {
                builder = group.MapGet(route, () => Results.Ok());
            }

            return builder;
        }
        """)]
    [InlineData("""
        {
            Func<RouteHandlerBuilder> map = () => group.MapGet("", () => Results.Ok());
            return map();
        }
        """)]
    [InlineData("""
        {
            return Local();

            RouteHandlerBuilder Local() => group.MapGet("", () => Results.Ok());
        }
        """)]
    [InlineData("=> Helper(group);")]
    [InlineData("""=> group.MapGroup("x").MapGet("", () => Results.Ok());""")]
    public async Task Analyze_WithMapNotMappingExactlyOneRoute_ReportsHmae007(string map)
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + $$"""
            internal sealed class ListBooksEndpoint : IEndpoint<BooksGroup>
            {
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group)
                {{map}}

                private static RouteHandlerBuilder Helper(IEndpointRouteBuilder builder) =>
                    builder.MapGet("", () => Results.Ok());
            }
            """);

        Assert.Empty(run.CompileErrors());
        Diagnostic diagnostic = Assert.Single(run.Diagnostics);
        Assert.Equal("HMAE007", diagnostic.Id);
        Assert.Equal("Map", run.SourceAt(diagnostic));
        Assert.StartsWith(
            "App.ListBooksEndpoint.Map must map exactly one route",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""=> group.MapGet("", () => Results.Ok());""")]
    [InlineData("""=> group.MapPost("", () => Results.Ok());""")]
    [InlineData("""=> group.MapPut("", () => Results.Ok());""")]
    [InlineData("""=> group.MapDelete("", () => Results.Ok());""")]
    [InlineData("""=> group.MapPatch("", () => Results.Ok());""")]
    [InlineData("""=> group.MapMethods("", ["HEAD"], () => Results.Ok());""")]
    [InlineData("""
        => group.MapGet("", () => Results.Ok())
            .WithSummary("Lists the books.")
            .RequireAuthorization();
        """)]
    [InlineData("""
        {
            RouteHandlerBuilder builder = group.MapGet("", () => Results.Ok());
            builder.WithSummary("Lists the books.");
            return builder;
        }
        """)]
    public async Task Analyze_WithMapMappingOneRoute_ReportsNothing(string map)
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + $$"""
            internal sealed class ListBooksEndpoint : IEndpoint<BooksGroup>
            {
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group)
                {{map}}
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Analyze_WithExplicitMapImplementation_ChecksIt()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal sealed class ListBooksEndpoint : IEndpoint<BooksGroup>
            {
                RouteHandlerBuilder IEndpoint.Map(IEndpointRouteBuilder group) =>
                    group.MapGet("", () => Results.Ok());
            }

            internal sealed class CountBooksEndpoint : IEndpoint<BooksGroup>
            {
                RouteHandlerBuilder IEndpoint.Map(IEndpointRouteBuilder group) => null!;
            }
            """);

        Assert.Empty(run.CompileErrors());
        Diagnostic diagnostic = Assert.Single(run.Diagnostics);
        Assert.Equal("HMAE007", diagnostic.Id);
        Assert.Contains(
            "App.CountBooksEndpoint",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analyze_WithMapOverriddenFromBaseClass_ChecksTheOverride()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal abstract class BooksEndpoint<T> : IEndpoint<BooksGroup>
            {
                public abstract RouteHandlerBuilder Map(IEndpointRouteBuilder group);
            }

            internal sealed class CountBooksEndpoint : BooksEndpoint<int>
            {
                public override RouteHandlerBuilder Map(IEndpointRouteBuilder group) => null!;
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Equal(["HMAE007"], run.DiagnosticIds);
    }

    [Fact]
    public async Task Analyze_WithMapMethodOutsideAnEndpoint_ReportsNothing()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal sealed class Routes
            {
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group) => null!;
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Analyze_WithAnyHandler_ReportsNothing()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal sealed class PingEndpoint : IEndpoint<BooksGroup>
            {
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                    group.MapGet("ping", () => Results.Ok());
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }
}
