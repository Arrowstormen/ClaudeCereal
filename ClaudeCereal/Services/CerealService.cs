using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class CerealService(AppDbContext db) : ICerealService
{
    private const int MaxPageSize = 100;

    public async Task<IEnumerable<Cereal>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(page, 1);

        return await db.Cereals
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Cereal?> GetByIdAsync(int id) =>
        await db.Cereals
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cereal> CreateAsync(Cereal cereal)
    {
        db.Cereals.Add(cereal);
        await db.SaveChangesAsync();
        return cereal;
    }

    public async Task<Cereal?> UpdateAsync(int id, Cereal input)
    {
        var cereal = await db.Cereals.FindAsync(id);
        if (cereal is null) return null;

        cereal.Name     = input.Name;
        cereal.Mfr      = input.Mfr;
        cereal.Type     = input.Type;
        cereal.Calories = input.Calories;
        cereal.Protein  = input.Protein;
        cereal.Fat      = input.Fat;
        cereal.Sodium   = input.Sodium;
        cereal.Fiber    = input.Fiber;
        cereal.Carbo    = input.Carbo;
        cereal.Sugars   = input.Sugars;
        cereal.Potass   = input.Potass;
        cereal.Vitamins = input.Vitamins;
        cereal.Shelf    = input.Shelf;
        cereal.Weight   = input.Weight;
        cereal.Cups     = input.Cups;
        cereal.Rating   = input.Rating;

        await db.SaveChangesAsync();
        return cereal;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = await db.Cereals
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync();

        return deleted > 0;
    }
}
