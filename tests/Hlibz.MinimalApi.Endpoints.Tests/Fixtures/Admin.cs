using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hlibz.MinimalApi.Endpoints.Tests.Fixtures.Admin;

/// <summary>
/// The app's own marker for admin groups, which a convention gates as a whole.
/// </summary>
internal interface IAdminEndpointGroup : IEndpointGroup;

internal sealed class AdminUsersEndpointGroup : IAdminEndpointGroup
{
    public string Prefix => "api/admin/users";
}

internal sealed class AdminReportsEndpointGroup : IEndpointGroup
{
    public string Prefix => "api/admin/reports";

    public string? AuthorizationPolicy => "Admin";

    public string? RateLimitPolicy => "Reports";

    public void Configure(RouteGroupBuilder builder)
    {
        builder.WithGroupName("admin");
    }
}

internal sealed class ListUsersEndpoint : IEndpoint<AdminUsersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => string.Empty;

    public Delegate Handler => () => Results.Ok();
}

internal sealed class PingUsersEndpoint : IEndpoint<AdminUsersEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "ping";

    public Delegate Handler => () => Results.Ok();

    public void Configure(RouteHandlerBuilder builder)
    {
        builder.AllowAnonymous();
    }
}

internal sealed class GetReportEndpoint : IEndpoint<AdminReportsEndpointGroup>
{
    public HttpMethod Method => HttpMethod.Get;

    public string Route => "{id}";

    public Delegate Handler => (string id) => Results.Ok(id);
}
