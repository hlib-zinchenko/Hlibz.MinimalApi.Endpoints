using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Admin;
using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders;

namespace Hlibz.MinimalApi.Endpoints.Tests;

public sealed class EndpointDiscoveryTests
{
    [Fact]
    public void Discover_WithEndpoints_GroupsThemUnderTheirGroup()
    {
        IReadOnlyList<EndpointGroupDescriptor> groups = Discover(
            "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Admin");

        Assert.Equal(
            [typeof(AdminUsersEndpointGroup), typeof(AdminReportsEndpointGroup)],
            groups.Select(group => group.GroupType).Order(TypeNameComparer.Instance).Reverse());
        EndpointGroupDescriptor users = groups.Single(
            group => group.GroupType == typeof(AdminUsersEndpointGroup));
        Assert.Equal(
            [typeof(ListUsersEndpoint), typeof(PingUsersEndpoint)],
            users.Endpoints.Select(endpoint => endpoint.EndpointType).Order(TypeNameComparer.Instance));
        Assert.Equal("api/admin/users", users.Group.Prefix);
    }

    [Fact]
    public void Discover_WithEndpoint_DescribesIt()
    {
        EndpointDescriptor endpoint = Discover("Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Orders")
            .SelectMany(group => group.Endpoints)
            .Single(endpoint => endpoint.EndpointType == typeof(GetOrderEndpoint));

        Assert.Equal(typeof(OrdersEndpointGroup), endpoint.GroupType);
        Assert.IsType<GetOrderEndpoint>(endpoint.Endpoint);
    }

    [Fact]
    public void Discover_WithAssemblyWithoutEndpointList_Throws()
    {
        // The library itself isn't compiled with the generator, so it has no endpoint list.
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => EndpointDiscovery.Discover([typeof(EndpointDiscovery).Assembly]));

        Assert.Contains("has no endpoint list", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_WithEndpointsNotIncluded_CreatesNeitherThemNorTheirGroups()
    {
        IReadOnlyList<EndpointGroupDescriptor> groups = EndpointDiscovery.Discover(
            [typeof(TestApp).Assembly],
            _ => false);

        Assert.Empty(groups);
    }

    [Fact]
    public void Discover_WithSameAssemblyTwice_FindsEachEndpointOnce()
    {
        IReadOnlyList<EndpointGroupDescriptor> groups = EndpointDiscovery.Discover(
            [typeof(TestApp).Assembly, typeof(TestApp).Assembly],
            type => type == typeof(GetOrderEndpoint));

        Assert.Single(Assert.Single(groups).Endpoints);
    }

    private static IReadOnlyList<EndpointGroupDescriptor> Discover(string fixtureNamespace) =>
        EndpointDiscovery.Discover(
            [typeof(TestApp).Assembly],
            type => TestApp.InNamespace(type, fixtureNamespace));

    private sealed class TypeNameComparer : IComparer<Type>
    {
        public static readonly TypeNameComparer Instance = new();

        public int Compare(Type? x, Type? y) =>
            string.CompareOrdinal(x?.Name, y?.Name);
    }
}
