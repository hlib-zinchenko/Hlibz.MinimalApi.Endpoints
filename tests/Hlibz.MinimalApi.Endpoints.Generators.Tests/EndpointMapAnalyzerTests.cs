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

        internal sealed record Other(int Id);

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
            internal abstract class BooksEndpoint<TResponse> : IEndpoint<BooksGroup, TResponse>
            {
                public abstract RouteHandlerBuilder Map(IEndpointRouteBuilder group);
            }

            internal sealed class CountBooksEndpoint : BooksEndpoint<int>
            {
                public override RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                    group.MapGet("count", () => "many");
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Equal(["HMAE008"], run.DiagnosticIds);
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

    [Theory]
    [InlineData("() => TypedResults.NoContent()", "Microsoft.AspNetCore.Http.HttpResults.NoContent")]
    [InlineData("() => TypedResults.Ok(new Other(1))", "Microsoft.AspNetCore.Http.HttpResults.Ok<App.Other>")]
    [InlineData("() => { }", "void")]
    [InlineData("""() => TypedResults.File(new byte[1], "text/csv")""", "Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult")]
    [InlineData("HandleAsync", "System.Threading.Tasks.Task")]
    public async Task Analyze_WithHandlerNotReturningResponseType_ReportsHmae008(
        string handler,
        string returnType)
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + ResponseEndpoint("Book", handler));

        Assert.Empty(run.CompileErrors());
        Diagnostic diagnostic = Assert.Single(run.Diagnostics);
        Assert.Equal("HMAE008", diagnostic.Id);
        Assert.Equal(handler, run.SourceAt(diagnostic));
        Assert.StartsWith(
            $"App.GetBookEndpoint's handler returns {returnType}, which doesn't carry the "
            + "endpoint's response type App.Book",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("() => Results.Ok(new Book(1))", "returns Microsoft.AspNetCore.Http.IResult")]
    [InlineData("HandleResultAsync", "returns System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IResult>")]
    [InlineData("HandleObject", "returns object")]
    [InlineData("Handler", "isn't a method group or lambda")]
    public async Task Analyze_WithHandlerTheBuildCantCheck_ReportsHmae009(
        string handler,
        string reason)
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + ResponseEndpoint("Book", handler));

        Assert.Empty(run.CompileErrors());
        Diagnostic diagnostic = Assert.Single(run.Diagnostics);
        Assert.Equal("HMAE009", diagnostic.Id);
        Assert.Equal(handler, run.SourceAt(diagnostic));
        Assert.StartsWith(
            $"App.GetBookEndpoint declares the response type App.Book, but its handler {reason}",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Book", "() => new Book(1)")]
    [InlineData("Book", "() => Task.FromResult(new Book(1))")]
    [InlineData("Book", "() => ValueTask.FromResult(new Book(1))")]
    [InlineData("Book", "() => TypedResults.Ok(new Book(1))")]
    [InlineData("Book", """() => TypedResults.Created("/api/books/1", new Book(1))""")]
    [InlineData("Book", "HandleUnion")]
    [InlineData("Book", "HandleUnionAsync")]
    [InlineData("Book", "HandleNullable")]
    [InlineData("Book?", "() => TypedResults.Ok(new Book(1))")]
    [InlineData("Book[]", "() => TypedResults.Ok(new[] { new Book(1) })")]
    [InlineData("int", "() => TypedResults.Ok(1)")]
    public async Task Analyze_WithHandlerReturningResponseType_ReportsNothing(
        string response,
        string handler)
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + ResponseEndpoint(response, handler));

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Analyze_WithResponseTypeFromGenericBase_ComparesTheTypeParameter()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal abstract class GetEndpoint<TResponse> : IEndpoint<BooksGroup, TResponse>
                where TResponse : new()
            {
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                    group.MapGet("", () => TypedResults.Ok(new TResponse()));
            }

            internal sealed class GetOtherEndpoint : GetEndpoint<List<int>>;
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }

    [Fact]
    public async Task Analyze_WithoutResponseType_AcceptsAnyHandler()
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

    [Fact]
    public async Task Analyze_WithHmae009Suppressed_ReportsNothing()
    {
        AnalyzerRun run = await AnalyzerRun.OfAsync(Groups + """
            internal sealed class GetBookEndpoint : IEndpoint<BooksGroup, Book>
            {
            #pragma warning disable HMAE009 // Answers through a shared result helper.
                public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                    group.MapGet("", () => Results.Ok(new Book(1)));
            #pragma warning restore HMAE009
            }
            """);

        Assert.Empty(run.CompileErrors());
        Assert.Empty(run.Diagnostics);
    }

    private static string ResponseEndpoint(string response, string handler) => $$"""
        internal sealed class GetBookEndpoint : IEndpoint<BooksGroup, {{response}}>
        {
            private static Delegate Handler => () => new Book(1);

            public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                group.MapGet("", {{handler}});

            private static Task HandleAsync() => Task.CompletedTask;

            private static Task<IResult> HandleResultAsync() =>
                Task.FromResult<IResult>(Results.Ok(new Book(1)));

            private static object HandleObject() => new Book(1);

            private static Book? HandleNullable() => null;

            private static Results<Ok<Book>, NotFound> HandleUnion(int id) =>
                id > 0 ? TypedResults.Ok(new Book(id)) : TypedResults.NotFound();

            private static async Task<Results<Ok<Book>, NotFound>> HandleUnionAsync(int id)
            {
                await Task.Yield();
                return TypedResults.NotFound();
            }
        }
        """;
}
