using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cereal> Cereals => Set<Cereal>();
}
