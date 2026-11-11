using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Library.Api;

/// <summary>
/// Demo-only sign-in: <c>X-User: alice</c> and <c>X-Role: member</c> (or <c>admin</c>). Use
/// a real scheme (JWT bearer, cookies) in an actual app.
/// </summary>
public sealed class HeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Header";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? user = Request.Headers["X-User"];
        if (string.IsNullOrWhiteSpace(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new(
            [
                new Claim(ClaimTypes.Name, user),
                new Claim(ClaimTypes.Role, Request.Headers["X-Role"].ToString()),
            ],
            SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
