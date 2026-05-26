using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class CerealService(AppDbContext db) : ICerealService
{
    public async Task<IEnumerable<Cereal>> GetFilteredAsync(CerealFilter filter)
    {
        var query = db.Cereals.AsNoTracking().AsQueryable();

        // Name
        if (filter.Name is not null)
            query = query.Where(c => c.Name == filter.Name);
        if (filter.NameContains is not null)
            query = query.Where(c => c.Name.Contains(filter.NameContains));
        // Categorical
        if (filter.Manufacturer.HasValue)
            query = query.Where(c => c.Mfr == filter.Manufacturer);
        if (filter.Type.HasValue)
            query = query.Where(c => c.Type == filter.Type);
        if (filter.Shelf.HasValue)
            query = query.Where(c => c.Shelf == filter.Shelf);
        // Nutrition ranges
        if (filter.MinCalories.HasValue)
            query = query.Where(c => c.Calories >= filter.MinCalories);
        if (filter.MaxCalories.HasValue)
            query = query.Where(c => c.Calories <= filter.MaxCalories);
        if (filter.MinProtein.HasValue)
            query = query.Where(c => c.Protein >= filter.MinProtein);
        if (filter.MaxProtein.HasValue)
            query = query.Where(c => c.Protein <= filter.MaxProtein);
        if (filter.MinFat.HasValue)
            query = query.Where(c => c.Fat >= filter.MinFat);
        if (filter.MaxFat.HasValue)
            query = query.Where(c => c.Fat <= filter.MaxFat);
        if (filter.MinSodium.HasValue)
            query = query.Where(c => c.Sodium >= filter.MinSodium);
        if (filter.MaxSodium.HasValue)
            query = query.Where(c => c.Sodium <= filter.MaxSodium);
        if (filter.MinFiber.HasValue)
            query = query.Where(c => c.Fiber >= filter.MinFiber);
        if (filter.MaxFiber.HasValue)
            query = query.Where(c => c.Fiber <= filter.MaxFiber);
        if (filter.MinCarbo.HasValue)
            query = query.Where(c => c.Carbo >= filter.MinCarbo);
        if (filter.MaxCarbo.HasValue)
            query = query.Where(c => c.Carbo <= filter.MaxCarbo);
        if (filter.MinSugars.HasValue)
            query = query.Where(c => c.Sugars >= filter.MinSugars);
        if (filter.MaxSugars.HasValue)
            query = query.Where(c => c.Sugars <= filter.MaxSugars);
        if (filter.MinPotass.HasValue)
            query = query.Where(c => c.Potass >= filter.MinPotass);
        if (filter.MaxPotass.HasValue)
            query = query.Where(c => c.Potass <= filter.MaxPotass);
        if (filter.MinVitamins.HasValue)
            query = query.Where(c => c.Vitamins >= filter.MinVitamins);
        if (filter.MaxVitamins.HasValue)
            query = query.Where(c => c.Vitamins <= filter.MaxVitamins);
        // Serving size ranges
        if (filter.MinWeight.HasValue)
            query = query.Where(c => c.Weight >= filter.MinWeight);
        if (filter.MaxWeight.HasValue)
            query = query.Where(c => c.Weight <= filter.MaxWeight);
        if (filter.MinCups.HasValue)
            query = query.Where(c => c.Cups >= filter.MinCups);
        if (filter.MaxCups.HasValue)
            query = query.Where(c => c.Cups <= filter.MaxCups);
        // Rating range
        if (filter.MinRating.HasValue)
            query = query.Where(c => c.Rating >= filter.MinRating);
        if (filter.MaxRating.HasValue)
            query = query.Where(c => c.Rating <= filter.MaxRating);

        // Sorting
        bool desc = filter.SortOrder == SortOrder.Desc;
        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "name"     => desc ? query.OrderByDescending(c => c.Name)     : query.OrderBy(c => c.Name),
            "calories" => desc ? query.OrderByDescending(c => c.Calories) : query.OrderBy(c => c.Calories),
            "protein"  => desc ? query.OrderByDescending(c => c.Protein)  : query.OrderBy(c => c.Protein),
            "fat"      => desc ? query.OrderByDescending(c => c.Fat)      : query.OrderBy(c => c.Fat),
            "sodium"   => desc ? query.OrderByDescending(c => c.Sodium)   : query.OrderBy(c => c.Sodium),
            "fiber"    => desc ? query.OrderByDescending(c => c.Fiber)    : query.OrderBy(c => c.Fiber),
            "carbo"    => desc ? query.OrderByDescending(c => c.Carbo)    : query.OrderBy(c => c.Carbo),
            "sugars"   => desc ? query.OrderByDescending(c => c.Sugars)   : query.OrderBy(c => c.Sugars),
            "potass"   => desc ? query.OrderByDescending(c => c.Potass)   : query.OrderBy(c => c.Potass),
            "vitamins" => desc ? query.OrderByDescending(c => c.Vitamins) : query.OrderBy(c => c.Vitamins),
            "shelf"    => desc ? query.OrderByDescending(c => c.Shelf)    : query.OrderBy(c => c.Shelf),
            "weight"   => desc ? query.OrderByDescending(c => c.Weight)   : query.OrderBy(c => c.Weight),
            "cups"     => desc ? query.OrderByDescending(c => c.Cups)     : query.OrderBy(c => c.Cups),
            "rating"   => desc ? query.OrderByDescending(c => c.Rating)   : query.OrderBy(c => c.Rating),
            _          => query.OrderBy(c => c.Id)
        };

        return await query.ToListAsync();
    }

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
