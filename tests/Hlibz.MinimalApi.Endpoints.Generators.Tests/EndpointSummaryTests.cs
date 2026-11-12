using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators.Tests;

public sealed class EndpointSummaryTests
{
    private const string Groups = """
        namespace App;

        internal sealed class OrdersGroup : IEndpointGroup
        {
            public string Prefix => "api/orders";
        }

        internal sealed record OrderDto(int Id);

        """;

    [Theory]
    [InlineData(DocumentationMode.Parse)]
    [InlineData(DocumentationMode.Diagnose)]
    [InlineData(DocumentationMode.None)]
    public void Generate_WithSummary_ListsItAsCommonMarkDescription(DocumentationMode mode)
    {
        string? description = DescriptionOf(
            """
            /// <summary>
            /// Gets an <see cref="OrderDto"/> (or <see langword="null"/>) &amp; a <c>404</c>
            /// for <paramref name="id"/>, like <see cref="List{T}"/>,
            /// <see cref="M:App.OrderDto.Find(System.Int32)"/>,
            /// <see cref="OrderDto">the order</see> and "quoted" \ text.
            /// <para>
            /// Needs the <see href="https://example.com/roles">Member role</see>, see
            /// <see href="https://example.com"/>.
            /// </para>
            /// </summary>
            /// <remarks>Implementation notes stay out.</remarks>
            """,
            mode);

        Assert.Equal(
            "Gets an `OrderDto` (or `null`) & a `404` for `id`, like `List<T>`, `Find`, "
            + "the order and \"quoted\" \\ text.\n\n"
            + "Needs the [Member role](https://example.com/roles), see https://example.com.",
            description);
    }

    [Theory]
    [InlineData("/** <summary>Block form.</summary> */", "Block form.")]
    [InlineData(
        """
        /**
         * <summary>
         * Starred lines.
         * </summary>
         */
        """,
        "Starred lines.")]
    [InlineData(
        """
        /// <summary>
        /// <list type="bullet">
        /// <item><description>One.</description></item>
        /// <item><description>Two.</description></item>
        /// </list>
        /// </summary>
        """,
        "- One.\n\n- Two.")]
    [InlineData(
        """
        /// <summary>
        /// Run:
        /// <code>
        /// dotnet run
        ///   --project api
        /// </code>
        /// </summary>
        """,
        "Run:\n\n```\ndotnet run\n  --project api\n```")]
    [InlineData(
        """
        /// <summary>Old.</summary>
        // A plain comment in between.
        [Obsolete]
        """,
        "Old.")]
    public void Generate_WithSummaryShape_RendersIt(string comment, string expected)
    {
        Assert.Equal(expected, DescriptionOf(comment));
    }

    [Theory]
    [InlineData("")]
    [InlineData("// <summary>A plain comment.</summary>")]
    [InlineData("//// <summary>Commented out.</summary>")]
    [InlineData("/// <summary>Never closed.")]
    [InlineData("/// <remarks>Only remarks.</remarks>")]
    [InlineData("/// <summary>   </summary>")]
    [InlineData("/// <inheritdoc/>")]
    public void Generate_WithoutUsableSummary_ListsNoDescription(string comment)
    {
        Assert.Null(DescriptionOf(comment));
    }

    [Fact]
    public void Generate_WithSummaryOnAnotherPartialDeclaration_ListsIt()
    {
        GeneratorRun run = GeneratorRun.Of(
        [
            Groups + """
                internal sealed partial class GetOrderEndpoint : IEndpoint<OrdersGroup>
                {
                    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                        group.MapGet("", () => Results.Ok());
                }
                """,
            """
            namespace App;

            /// <summary>From the other part.</summary>
            internal sealed partial class GetOrderEndpoint;
            """,
        ]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Equal("From the other part.", SingleEndpoint(run).Description);
    }

    private static string? DescriptionOf(
        string comment,
        DocumentationMode mode = DocumentationMode.Parse)
    {
        GeneratorRun run = GeneratorRun.Of(
            [
                Groups + $$"""
                    {{comment}}
                    internal sealed class GetOrderEndpoint : IEndpoint<OrdersGroup>
                    {
                        public RouteHandlerBuilder Map(IEndpointRouteBuilder group) =>
                            group.MapGet("", () => Results.Ok());
                    }
                    """,
            ],
            documentationMode: mode);

        Assert.Empty(run.GeneratorDiagnostics);
        return SingleEndpoint(run).Description;
    }

    private static EndpointDescriptor SingleEndpoint(GeneratorRun run) =>
        Assert.Single(Assert.Single(EndpointDiscovery.Discover([run.Load()])).Endpoints);
}
