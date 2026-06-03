using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeCereal.Tests.Helpers;

/// <summary>
/// A test-only authentication handler that reads auth state from request headers
/// instead of verifying credentials. Removing the need for real Basic Auth in
/// integration tests while still exercising the full authorization pipeline.
/// <list type="bullet">
///   <item><c>X-Test-Unauthenticated: true</c> — simulates an unauthenticated request (returns 401)</item>
///   <item><c>X-Test-User: alice</c> — sets the username (defaults to "testuser")</item>
///   <item><c>X-Test-Roles: Admin,Editor</c> — sets comma-separated roles</item>
/// </list>
/// The <see cref="ClaudeCereal.Authentication.RoleHierarchyTransformation"/> still runs
/// automatically, so sending "Admin" also grants "Editor" and "Reader" claims.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory                               logger,
    UrlEncoder                                   encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName            = "Test";
    public const string HeaderUnauthenticated = "X-Test-Unauthenticated";
    public const string HeaderUser            = "X-Test-User";
    public const string HeaderRoles           = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(HeaderUnauthenticated))
            return Task.FromResult(AuthenticateResult.NoResult());

        var user  = Request.Headers[HeaderUser].FirstOrDefault() ?? "testuser";
        var roles = (Request.Headers[HeaderRoles].FirstOrDefault() ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = new List<Claim> { new(ClaimTypes.Name, user) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
