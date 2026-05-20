using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cereal> Cereals => Set<Cereal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cereal>(entity =>
        {
            entity.Property(c => c.Mfr).HasConversion<string>();
            entity.Property(c => c.Type).HasConversion<string>();
        });
    }
}
