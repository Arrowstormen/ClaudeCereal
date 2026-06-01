using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task<PagedResult<AuditLog>> GetPagedAsync(
        AuditFilter       filter,
        CancellationToken cancellationToken = default)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.EntityId.HasValue)
            query = query.Where(a => a.EntityId == filter.EntityId.Value);
        if (filter.Action.HasValue)
            query = query.Where(a => a.Action == filter.Action.Value);
        if (filter.Actor is not null)
            query = query.Where(a => a.Actor == filter.Actor);
        if (filter.CorrelationId is not null)
            query = query.Where(a => a.CorrelationId == filter.CorrelationId);
        if (filter.From.HasValue)
            query = query.Where(a => a.Timestamp >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(a => a.Timestamp <= filter.To.Value);

        // Most-recent entries first
        query = query.OrderByDescending(a => a.Timestamp);

        int p  = Math.Max(1, filter.Page ?? 1);
        int ps = Math.Clamp(filter.PageSize ?? 20, 1, 100);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(
            items, p, ps, total,
            (int)Math.Ceiling(total / (double)ps));
    }
}
