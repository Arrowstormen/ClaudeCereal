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

    public async Task<Cereal> CreateAsync(CerealDto dto)
    {
        var cereal = new Cereal();
        MapToEntity(dto, cereal);
        db.Cereals.Add(cereal);
        await db.SaveChangesAsync();
        return cereal;
    }

    public async Task<Cereal?> UpdateAsync(int id, CerealDto dto)
    {
        var cereal = await db.Cereals.FindAsync(id);
        if (cereal is null) return null;

        MapToEntity(dto, cereal);
        await db.SaveChangesAsync();
        return cereal;
    }

    private static void MapToEntity(CerealDto dto, Cereal target)
    {
        target.Name     = dto.Name;
        target.Mfr      = dto.Mfr;
        target.Type     = dto.Type;
        target.Calories = dto.Calories;
        target.Protein  = dto.Protein;
        target.Fat      = dto.Fat;
        target.Sodium   = dto.Sodium;
        target.Fiber    = dto.Fiber;
        target.Carbo    = dto.Carbo;
        target.Sugars   = dto.Sugars;
        target.Potass   = dto.Potass;
        target.Vitamins = dto.Vitamins;
        target.Shelf    = dto.Shelf;
        target.Weight   = dto.Weight;
        target.Cups     = dto.Cups;
        target.Rating   = dto.Rating;
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
