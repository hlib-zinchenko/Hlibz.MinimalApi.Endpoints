using Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests;

public sealed class GroupConventionTests
{
    private const string Admin = "Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Admin";

    [Fact]
    public async Task MapEndpoints_WithTypedGroupConvention_AppliesItToMatchingGroupsOnly()
    {
        await using WebApplication app = await StartAdminAsync();

        IAuthorizeData users = Assert.Single(
            app.Endpoint("ListUsers").Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.Equal("Admin", users.Policy);
        Assert.Null(app.Endpoint("GetReport").Metadata.GetMetadata<DisableRateLimitingAttribute>());
    }

    [Fact]
    public async Task MapEndpoints_WithGroupConventionAuthorization_Documents401And403()
    {
        await using WebApplication app = await StartAdminAsync();
        RouteEndpoint endpoint = app.Endpoint("ListUsers");

        Assert.Single(endpoint.Responses(StatusCodes.Status401Unauthorized));
        Assert.Single(endpoint.Responses(StatusCodes.Status403Forbidden));
    }

    [Fact]
    public async Task MapEndpoints_WithAllowAnonymousEndpoint_DocumentsNo401()
    {
        await using WebApplication app = await StartAdminAsync();

        Assert.Empty(app.Endpoint("PingUsers").Responses(StatusCodes.Status401Unauthorized));
    }

    [Fact]
    public async Task MapEndpoints_WithGroupDisablingRateLimiting_DocumentsNo429()
    {
        await using WebApplication app = await StartAdminAsync(
            options => options.ConfigureEndpoints(context =>
                context.Builder.RequireRateLimiting("Global")));

        RouteEndpoint users = app.Endpoint("ListUsers");
        Assert.Empty(users.Responses(StatusCodes.Status429TooManyRequests));
    }

    [Fact]
    public async Task MapEndpoints_WithGroupPolicies_RequiresThemOnEveryEndpoint()
    {
        await using WebApplication app = await StartAdminAsync();
        RouteEndpoint endpoint = app.Endpoint("GetReport");

        Assert.Equal("Admin", endpoint.Metadata.GetMetadata<IAuthorizeData>()?.Policy);
        Assert.Equal(
            "Reports",
            endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName);
        Assert.Single(endpoint.Responses(StatusCodes.Status401Unauthorized));
        Assert.Single(endpoint.Responses(StatusCodes.Status429TooManyRequests));
    }

    [Fact]
    public async Task MapEndpoints_WithGroupConfigure_AppliesGroupsOwnSettings()
    {
        await using WebApplication app = await StartAdminAsync();

        IEndpointGroupNameMetadata? groupName =
            app.Endpoint("GetReport").Metadata.GetMetadata<IEndpointGroupNameMetadata>();

        Assert.Equal("admin", groupName?.EndpointGroupName);
    }

    [Fact]
    public async Task MapEndpoints_WithRejectingGroupConvention_FailsAtStartup()
    {
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TestApp.StartAsync(
                Admin,
                options => options.ConfigureGroups(context =>
                {
                    bool isAdmin = context.Group is IAdminEndpointGroup;
                    if (isAdmin != context.Group.Prefix.StartsWith("api/admin/", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"{context.GroupType.Name} is misplaced.");
                    }
                })));

        Assert.Equal("AdminReportsEndpointGroup is misplaced.", exception.Message);
    }

    private static Task<WebApplication> StartAdminAsync(
        Action<EndpointMappingOptions>? configure = null)
    {
        return TestApp.StartAsync(
            Admin,
            options =>
            {
                options.ConfigureGroups<IAdminEndpointGroup>((_, group) => group
                    .RequireAuthorization("Admin")
                    .DisableRateLimiting());
                configure?.Invoke(options);
            });
    }
}
