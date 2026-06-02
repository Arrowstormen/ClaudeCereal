using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
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
            entity.HasIndex(a => a.CorrelationId);
        });
    }

    // Block synchronous saves — the audit interceptor requires async to avoid
    // deadlocks in ASP.NET Core and to keep both saves within one transaction.
    public override int SaveChanges() =>
        throw new NotSupportedException("Use SaveChangesAsync to ensure audit logs are written.");

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Use SaveChangesAsync to ensure audit logs are written.");
}
