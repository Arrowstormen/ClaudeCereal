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

    public async Task<Cereal> CreateAsync(CerealRequest request)
    {
        var cereal = new Cereal();
        MapToEntity(request, cereal);
        db.Cereals.Add(cereal);
        await db.SaveChangesAsync();
        return cereal;
    }

    public async Task<Cereal?> UpdateAsync(int id, CerealRequest request)
    {
        var cereal = await db.Cereals.FindAsync(id);
        if (cereal is null) return null;

        // Tell EF Core to check the client's version in the SQL WHERE clause
        db.Entry(cereal).Property(c => c.Version).OriginalValue = request.Version;
        MapToEntity(request, cereal);
        cereal.Version++;

        // Throws DbUpdateConcurrencyException if another user already changed the row
        await db.SaveChangesAsync();
        return cereal;
    }

    private static void MapToEntity(CerealRequest request, Cereal target)
    {
        target.Name     = request.Name;
        target.Mfr      = request.Mfr;
        target.Type     = request.Type;
        target.Calories = request.Calories;
        target.Protein  = request.Protein;
        target.Fat      = request.Fat;
        target.Sodium   = request.Sodium;
        target.Fiber    = request.Fiber;
        target.Carbo    = request.Carbo;
        target.Sugars   = request.Sugars;
        target.Potass   = request.Potass;
        target.Vitamins = request.Vitamins;
        target.Shelf    = request.Shelf;
        target.Weight   = request.Weight;
        target.Cups     = request.Cups;
        target.Rating   = request.Rating;
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
