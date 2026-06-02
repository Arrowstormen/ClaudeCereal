using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClaudeCereal.Data;

/// <summary>
/// EF Core save-changes interceptor that captures field-level audit entries for every
/// entity implementing <see cref="IAuditable"/> and persists them atomically alongside
/// the originating change.
/// </summary>
public sealed class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    // Mutable state is safe because the interceptor is registered as Scoped (one per request).
    private List<PendingAuditEntry> _pendingEntries     = [];
    private IDbContextTransaction?  _transaction;
    private bool                    _isWritingAuditLogs;

    // -------------------------------------------------------------------------
    // Intercept points
    // -------------------------------------------------------------------------

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData      eventData,
        InterceptionResult<int> result,
        CancellationToken       cancellationToken = default)
    {
        // Re-entrancy guard: the second SaveChangesAsync that writes audit rows must
        // not start another capture/transaction cycle.
        if (_isWritingAuditLogs)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // Tamper protection — audit entries are immutable.
        if (context.ChangeTracker.Entries<AuditLog>()
                .Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException(
                "Audit log entries are immutable and cannot be modified or deleted.");

        _pendingEntries = CaptureAuditEntries(context);

        // Open a transaction only when there are changes to audit and no outer transaction
        // is already in progress.
        if (_pendingEntries.Count > 0 && context.Database.CurrentTransaction is null)
            _transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int                           result,
        CancellationToken             cancellationToken = default)
    {
        // Fast path: no auditable entities were changed, or we are in the re-entrant
        // audit-write pass.
        if (_isWritingAuditLogs || _pendingEntries.Count == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var actor         = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
        var correlationId = httpContextAccessor.HttpContext?.TraceIdentifier    ?? Guid.NewGuid().ToString();
        var now           = DateTime.UtcNow;

        // Entity IDs for Added rows are available here because the first save has
        // already committed them to the identity column.
        var auditLogs = _pendingEntries
            .Select(e => new AuditLog
            {
                Timestamp     = now,
                Actor         = actor,
                CorrelationId = correlationId,
                Action        = e.Action,
                EntityId      = (int)context.Entry((object)e.Entity).CurrentValues["Id"]!,
                EntityName    = e.EntityName,
                Changes       = e.Changes
            })
            .ToList();

        context.Set<AuditLog>().AddRange(auditLogs);

        _isWritingAuditLogs = true;
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _isWritingAuditLogs = false;
        }

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _pendingEntries = [];

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken       cancellationToken = default)
    {
        // Disposing without a preceding CommitAsync rolls the transaction back.
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _pendingEntries     = [];
        _isWritingAuditLogs = false;

        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Audit capture helpers
    // -------------------------------------------------------------------------

    // Properties excluded from field-level change tracking regardless of entity type.
    private static readonly HashSet<string> ExcludedProperties = [
        "Id",
        "Version",
        "DeletedAt"    // soft-delete state is encoded in the action type instead
    ];

    private static List<PendingAuditEntry> CaptureAuditEntries(DbContext context)
    {
        var entries = new List<PendingAuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            AuditAction            action;
            List<AuditFieldChange> changes;

            if (entry.State == EntityState.Added)
            {
                action  = AuditAction.Created;
                changes = entry.Properties
                    .Where(p => !ExcludedProperties.Contains(p.Metadata.Name)
                             && p.CurrentValue is not null)
                    .Select(p => new AuditFieldChange
                    {
                        Field    = p.Metadata.Name,
                        OldValue = null,
                        NewValue = p.CurrentValue?.ToString()
                    })
                    .ToList();
            }
            else
            {
                // Detect soft-delete / restore transitions via DeletedAt if present.
                var deletedAtProp = entry.Properties
                    .FirstOrDefault(p => p.Metadata.Name == "DeletedAt");

                var wasDeleted = deletedAtProp?.OriginalValue is not null;
                var isDeleted  = deletedAtProp?.CurrentValue  is not null;

                if (deletedAtProp is not null && !wasDeleted && isDeleted)
                {
                    action  = AuditAction.SoftDeleted;
                    changes = [];
                }
                else if (deletedAtProp is not null && wasDeleted && !isDeleted)
                {
                    action  = AuditAction.Restored;
                    changes = [];
                }
                else
                {
                    action  = AuditAction.Updated;
                    changes = entry.Properties
                        .Where(p => !ExcludedProperties.Contains(p.Metadata.Name)
                                 && p.IsModified)
                        .Select(p => new AuditFieldChange
                        {
                            Field    = p.Metadata.Name,
                            OldValue = p.OriginalValue?.ToString(),
                            NewValue = p.CurrentValue?.ToString()
                        })
                        .ToList();
                }
            }

            // Prefer a "Name" property for a human-readable entity name;
            // fall back to the EF metadata display name.
            var entityName =
                entry.Entity.GetType().GetProperty("Name")?.GetValue(entry.Entity) as string
                ?? entry.Metadata.DisplayName();

            entries.Add(new PendingAuditEntry(entry.Entity, action, entityName, changes));
        }

        return entries;
    }

    private record PendingAuditEntry(
        IAuditable             Entity,
        AuditAction            Action,
        string                 EntityName,
        List<AuditFieldChange> Changes);
}
