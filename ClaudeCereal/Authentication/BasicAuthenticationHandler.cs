using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClaudeCereal.Authentication;

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            header.Parameter is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Base64 encoding."));
        }

        var separatorIndex = credentials.IndexOf(':');
        if (separatorIndex < 0)
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials format."));

        var username = credentials[..separatorIndex];
        var password = credentials[(separatorIndex + 1)..];

        var expectedUsername = configuration["BasicAuth:Username"];
        var expectedPassword = configuration["BasicAuth:Password"];

        if (username != expectedUsername || password != expectedPassword)
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], Scheme.Name));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = $"Basic realm=\"ClaudeCereal\"";
        return base.HandleChallengeAsync(properties);
    }
}
