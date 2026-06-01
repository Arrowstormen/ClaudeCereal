using ClaudeCereal.Authentication;
using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/audit")
            .RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/", async (
            int?         entityId,
            AuditAction? action,
            int?         page,
            int?         pageSize,
            AppDbContext db) =>
        {
            var query = db.AuditLogs.AsNoTracking().AsQueryable();

            if (entityId.HasValue)
                query = query.Where(a => a.EntityId == entityId.Value);
            if (action.HasValue)
                query = query.Where(a => a.Action == action.Value);

            // Most-recent entries first
            query = query.OrderByDescending(a => a.Timestamp);

            int p  = Math.Max(1, page ?? 1);
            int ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var total = await query.CountAsync();
            var items = await query
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            return Results.Ok(new PagedResult<AuditLog>(
                items, p, ps, total,
                (int)Math.Ceiling(total / (double)ps)));
        });
    }
}
