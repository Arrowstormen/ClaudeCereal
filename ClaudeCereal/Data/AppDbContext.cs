using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    public DbSet<Cereal>   Cereals   => Set<Cereal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cereal>(entity =>
        {
            entity.Property(c => c.Mfr).HasConversion<string>();
            entity.Property(c => c.Type).HasConversion<string>();
            entity.Property(c => c.Version).IsConcurrencyToken();

            // Soft delete — active rows always exclude DeletedAt != null
            entity.HasQueryFilter(c => c.DeletedAt == null);

            // Index speeds up the common "WHERE DeletedAt IS NULL" predicate
            entity.HasIndex(c => c.DeletedAt);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.Action).HasConversion<string>();

            // Store field-level changes as a JSON array in a single TEXT column
            entity.OwnsMany(a => a.Changes, b => b.ToJson());

            entity.HasIndex(a => a.EntityId);
            entity.HasIndex(a => a.Timestamp);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture state before saving — ChangeTracker resets entries after base.SaveChangesAsync
        var pendingEntries = CaptureAuditEntries();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Write audit logs in a second save so that auto-generated entity IDs are available
        if (pendingEntries.Count > 0)
        {
            var actor = httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
            var now   = DateTime.UtcNow;

            AuditLogs.AddRange(pendingEntries.Select(e => new AuditLog
            {
                Timestamp  = now,
                Actor      = actor,
                Action     = e.Action,
                EntityId   = e.Entity.Id,   // populated by EF after the first save
                EntityName = e.EntityName,
                Changes    = e.Changes
            }));

            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    // Properties excluded from field-level change tracking
    private static readonly HashSet<string> ExcludedProperties = [
        nameof(Cereal.Id),
        nameof(Cereal.Version),
        nameof(Cereal.DeletedAt)     // action type already encodes delete / restore
    ];

    private List<PendingAuditEntry> CaptureAuditEntries()
    {
        var entries = new List<PendingAuditEntry>();

        foreach (var entry in ChangeTracker.Entries<Cereal>())
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
                var deletedAt  = entry.Property(nameof(Cereal.DeletedAt));
                var wasDeleted = deletedAt.OriginalValue is not null;
                var isDeleted  = deletedAt.CurrentValue  is not null;

                if (!wasDeleted && isDeleted)
                {
                    action  = AuditAction.SoftDeleted;
                    changes = [];
                }
                else if (wasDeleted && !isDeleted)
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

            entries.Add(new PendingAuditEntry(entry.Entity, action, entry.Entity.Name, changes));
        }

        return entries;
    }

    private record PendingAuditEntry(
        Cereal                 Entity,
        AuditAction            Action,
        string                 EntityName,
        List<AuditFieldChange> Changes);
}
