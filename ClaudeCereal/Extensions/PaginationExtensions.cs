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
        int normalizedPage     = Math.Max(1, page ?? 1);
        int normalizedPageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            items, normalizedPage, normalizedPageSize, total,
            (int)Math.Ceiling(total / (double)normalizedPageSize));
    }
}
