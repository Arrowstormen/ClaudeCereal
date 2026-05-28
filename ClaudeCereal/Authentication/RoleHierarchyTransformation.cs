using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace ClaudeCereal.Authentication;

/// <summary>
/// Expands role claims upward so higher roles automatically inherit lower ones.
/// Admin → also gets Editor and Reader.
/// Editor → also gets Reader.
/// This means policies only need to check for a single role rather than listing
/// every role that should satisfy the requirement.
/// </summary>
public class RoleHierarchyTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.IsInRole(Roles.Admin))
            AddRole(principal, Roles.Editor);

        if (principal.IsInRole(Roles.Editor))
            AddRole(principal, Roles.Reader);

        return Task.FromResult(principal);
    }

    private static void AddRole(ClaimsPrincipal principal, string role)
    {
        if (!principal.IsInRole(role))
            ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.Role, role));
    }
}
