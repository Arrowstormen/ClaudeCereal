using ClaudeCereal.Authentication;
using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClaudeCereal.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/audit")
            .RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/", async (
            [AsParameters] AuditFilter filter,
            IAuditService              auditService,
            CancellationToken          ct) =>
        {
            var result = await auditService.GetPagedAsync(filter, ct);
            return Results.Ok(result);
        });
    }
}
