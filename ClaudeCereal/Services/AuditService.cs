using ClaudeCereal.Data;
using ClaudeCereal.Extensions;
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

        return await query.ToPagedResultAsync(filter.Page, filter.PageSize, cancellationToken);
    }
}
