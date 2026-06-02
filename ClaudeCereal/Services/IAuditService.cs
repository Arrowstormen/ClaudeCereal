using ClaudeCereal.Models;

namespace ClaudeCereal.Services;

public interface IAuditService
{
    Task<PagedResult<AuditLog>> GetPagedAsync(
        AuditFilter       filter,
        CancellationToken cancellationToken = default);
}
