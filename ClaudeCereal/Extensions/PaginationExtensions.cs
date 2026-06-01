using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Extensions;

internal static class PaginationExtensions
{
    internal const int DefaultPageSize = 20;
    internal const int MaxPageSize     = 100;

    /// <summary>
    /// Executes <paramref name="query"/> with pagination applied and returns a
    /// <see cref="PagedResult{T}"/> containing the requested page.
    /// </summary>
    internal static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int?              page,
        int?              pageSize,
        CancellationToken cancellationToken = default)
    {
        int p  = Math.Max(1, page ?? 1);
        int ps = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            items, p, ps, total,
            (int)Math.Ceiling(total / (double)ps));
    }
}
