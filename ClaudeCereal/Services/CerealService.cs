using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class CerealService(AppDbContext db) : ICerealService
{
    public async Task<IEnumerable<Cereal>> GetAllAsync() =>
        await db.Cereals.ToListAsync();

    public async Task<Cereal?> GetByIdAsync(int id) =>
        await db.Cereals.FindAsync(id);

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
        var cereal = await db.Cereals.FindAsync(id);
        if (cereal is null) return false;

        db.Cereals.Remove(cereal);
        await db.SaveChangesAsync();
        return true;
    }
}
