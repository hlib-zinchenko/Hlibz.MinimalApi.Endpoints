using System.Globalization;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace Hlibz.MinimalApi.Endpoints.Generators.Tests;

public sealed class EndpointManifestGeneratorTests
{
    private const string Groups = """
        namespace App;

        internal sealed class OrdersGroup : IEndpointGroup
        {
            public string Prefix => "api/orders";
        }

        internal sealed record OrderDto(int Id);

        """;

    [Fact]
    public void Generate_WithEndpoints_ListsThemForDiscovery()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class GetOrderEndpoint : IEndpoint<OrdersGroup, OrderDto>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => "{id:int}";
                public Delegate Handler => (int id) => Results.Ok(new OrderDto(id));
            }

            internal sealed class PingOrdersEndpoint : IEndpoint<OrdersGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => "ping";
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assembly assembly = run.Load();
        EndpointGroupDescriptor group = Assert.Single(EndpointDiscovery.Discover([assembly]));
        Assert.Equal("App.OrdersGroup", group.GroupType.FullName);
        Assert.Equal("api/orders", group.Group.Prefix);
        Assert.Equal(
            ["App.GetOrderEndpoint", "App.PingOrdersEndpoint"],
            group.Endpoints.Select(endpoint => endpoint.EndpointType.FullName));
        Assert.Equal(
            ["App.OrderDto", null],
            group.Endpoints.Select(endpoint => endpoint.ResponseType?.FullName));
        Assert.Equal("{id:int}", group.Endpoints[0].Endpoint.Route);
    }

    [Fact]
    public void Generate_WithEndpointThroughBaseClass_ListsOnlyTheConcreteEndpoint()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal abstract class OrdersEndpoint<TResponse> : IEndpoint<OrdersGroup, TResponse>
            {
                public HttpMethod Method => HttpMethod.Get;
                public abstract string Route { get; }
                public abstract Delegate Handler { get; }
            }

            internal sealed class CountOrdersEndpoint : OrdersEndpoint<int>
            {
                public override string Route => "count";
                public override Delegate Handler => () => Results.Ok(1);
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        EndpointDescriptor endpoint = Assert.Single(
            Assert.Single(EndpointDiscovery.Discover([run.Load()])).Endpoints);
        Assert.Equal("App.CountOrdersEndpoint", endpoint.EndpointType.FullName);
        Assert.Equal(typeof(int), endpoint.ResponseType);
    }

    [Fact]
    public void Generate_WithPartialEndpointDeclaredTwice_ListsItOnce()
    {
        GeneratorRun run = GeneratorRun.Of(
        [
            Groups + """
                internal sealed partial class ListOrdersEndpoint : IEndpoint<OrdersGroup>
                {
                    public HttpMethod Method => HttpMethod.Get;
                    public string Route => string.Empty;
                }
                """,
            """
                namespace App;

                internal sealed partial class ListOrdersEndpoint : IEndpoint
                {
                    public Delegate Handler => () => Results.Ok();
                }
                """,
        ]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Single(Assert.Single(EndpointDiscovery.Discover([run.Load()])).Endpoints);
    }

    [Theory]
    [InlineData("OrderDto?", "App.OrderDto")]
    [InlineData("(int Id, string Name)", "System.ValueTuple`2[System.Int32,System.String]")]
    [InlineData("List<OrderDto?>[]", "System.Collections.Generic.List`1[App.OrderDto][]")]
    [InlineData("int?", "System.Nullable`1[System.Int32]")]
    public void Generate_WithResponseTypeSpelledDifferently_ListsTheRuntimeType(
        string responseType,
        string expected)
    {
        GeneratorRun run = GeneratorRun.Of(Groups + $$"""
            internal sealed class GetOrderEndpoint : IEndpoint<OrdersGroup, {{responseType}}>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Type? response = Assert.Single(
            Assert.Single(EndpointDiscovery.Discover([run.Load()])).Endpoints).ResponseType;
        Assert.Equal(expected, response?.ToString());
    }

    [Fact]
    public void Generate_WithInternalConstructors_CreatesEndpointAndGroup()
    {
        GeneratorRun run = GeneratorRun.Of("""
            namespace App;

            public sealed class OrdersGroup : IEndpointGroup
            {
                internal OrdersGroup()
                {
                }

                public string Prefix => "api/orders";
            }

            public sealed class ListOrdersEndpoint : IEndpoint<OrdersGroup>
            {
                internal ListOrdersEndpoint()
                {
                }

                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Single(EndpointDiscovery.Discover([run.Load()]));
    }

    [Fact]
    public void Generate_WithNoEndpoints_WritesAnEmptyList()
    {
        GeneratorRun run = GeneratorRun.Of("namespace App; internal sealed class Plain;");

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(EndpointDiscovery.Discover([run.Load()]));
    }

    [Fact]
    public void Generate_WithoutTheLibrary_WritesNothing()
    {
        GeneratorRun run = GeneratorRun.Of(
            "namespace App; internal sealed class Plain;",
            referenceLibrary: false);

        Assert.Null(run.Manifest);
    }

    [Fact]
    public void Generate_WithAssemblyName_NamesTheManifestAfterIt()
    {
        GeneratorRun run = GeneratorRun.Of("namespace App; internal sealed class Plain;");

        Assert.Contains(
            "internal sealed class EndpointManifest_Generated_Endpoints_1 ",
            run.Manifest,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithObsoleteEndpoint_ListsItWithoutWarnings()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            [Obsolete("Use v2.")]
            internal sealed class OldOrdersEndpoint : IEndpoint<OrdersGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => "old";
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.DoesNotContain(
            run.Output.GetDiagnostics(TestContext.Current.CancellationToken),
            item => item.Severity >= DiagnosticSeverity.Warning
                    && item.Location.SourceTree?.FilePath.EndsWith(
                        ".g.cs",
                        StringComparison.Ordinal) == true);
        Assert.Single(EndpointDiscovery.Discover([run.Load()]));
    }

    [Fact]
    public void Generate_WithEndpointImplementingIEndpointDirectly_ReportsHmae001()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class DirectEndpoint : IEndpoint
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("HMAE001", diagnostic.Id);
        Assert.Contains(
            "App.DirectEndpoint implements IEndpoint directly",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
        Assert.Equal("DirectEndpoint", run.SourceAt(diagnostic));
        Assert.Empty(run.CompileErrors());
    }

    [Fact]
    public void Generate_WithEndpointNamingTwoGroups_ReportsHmae002()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class ReportsGroup : IEndpointGroup
            {
                public string Prefix => "api/reports";
            }

            internal sealed class TwoGroupsEndpoint
                : IEndpoint<OrdersGroup>, IEndpoint<ReportsGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Equal(["HMAE002"], run.DiagnosticIds);
        Assert.Contains(
            "App.OrdersGroup, App.ReportsGroup",
            run.GeneratorDiagnostics[0].GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithEndpointNamingTwoResponseTypes_ReportsHmae003()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class TwoResponsesEndpoint
                : IEndpoint<OrdersGroup, OrderDto>, IEndpoint<OrdersGroup, int>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Equal(["HMAE003"], run.DiagnosticIds);
    }

    [Theory]
    [InlineData("private ListOrdersEndpoint() { }")]
    [InlineData("public ListOrdersEndpoint(string route) { }")]
    public void Generate_WithEndpointWithoutUsableConstructor_ReportsHmae004(string constructor)
    {
        GeneratorRun run = GeneratorRun.Of(Groups + $$"""
            internal sealed class ListOrdersEndpoint : IEndpoint<OrdersGroup>
            {
                {{constructor}}
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Equal(["HMAE004"], run.DiagnosticIds);
        Assert.Contains(
            "App.ListOrdersEndpoint has no public or internal parameterless constructor",
            run.GeneratorDiagnostics[0].GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithGroupWithoutParameterlessConstructor_ReportsHmae004()
    {
        GeneratorRun run = GeneratorRun.Of("""
            namespace App;

            internal sealed class NeedsPrefixGroup(string prefix) : IEndpointGroup
            {
                public string Prefix => prefix;
            }

            internal sealed class ListThingsEndpoint : IEndpoint<NeedsPrefixGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Equal(["HMAE004"], run.DiagnosticIds);
        Assert.Contains(
            "App.NeedsPrefixGroup",
            run.GeneratorDiagnostics[0].GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
        Assert.Equal("ListThingsEndpoint", run.SourceAt(run.GeneratorDiagnostics[0]));
    }

    [Theory]
    [InlineData("internal interface IAdminGroup : IEndpointGroup;")]
    [InlineData(
        "internal abstract class IAdminGroup : IEndpointGroup { public string Prefix => \"a\"; }")]
    public void Generate_WithAbstractGroup_ReportsHmae005(string group)
    {
        GeneratorRun run = GeneratorRun.Of($$"""
            namespace App;

            {{group}}

            internal sealed class ListUsersEndpoint : IEndpoint<IAdminGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Equal(["HMAE005"], run.DiagnosticIds);
    }

    [Fact]
    public void Generate_WithPrivateNestedEndpoint_ReportsHmae006()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal static class Outer
            {
                private sealed class HiddenEndpoint : IEndpoint<OrdersGroup>
                {
                    public HttpMethod Method => HttpMethod.Get;
                    public string Route => string.Empty;
                    public Delegate Handler => () => Results.Ok();
                }
            }
            """);

        Assert.Equal(["HMAE006"], run.DiagnosticIds);
        Assert.Contains(
            "App.Outer.HiddenEndpoint",
            run.GeneratorDiagnostics[0].GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithPrivateNestedResponseType_ReportsHmae006()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class GetSecretEndpoint
                : IEndpoint<OrdersGroup, GetSecretEndpoint.Secret>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();

                private sealed record Secret(string Value);
            }
            """);

        Assert.Contains("HMAE006", run.DiagnosticIds);
    }

    [Fact]
    public void Generate_WithInvalidEndpoint_StillListsTheValidOnes()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class DirectEndpoint : IEndpoint
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }

            internal sealed class ListOrdersEndpoint : IEndpoint<OrdersGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Contains("App.ListOrdersEndpoint", run.Manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("App.DirectEndpoint", run.Manifest, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithUnresolvedGroup_LeavesTheErrorToTheCompiler()
    {
        GeneratorRun run = GeneratorRun.Of(Groups + """
            internal sealed class ListOrdersEndpoint : IEndpoint<MissingGroup>
            {
                public HttpMethod Method => HttpMethod.Get;
                public string Route => string.Empty;
                public Delegate Handler => () => Results.Ok();
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Contains(run.CompileErrors(), error => error.Id == "CS0246");
    }
}
