using ClaudeCereal.Data;
using ClaudeCereal.Import;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class CerealService(AppDbContext db) : ICerealService
{
    public async Task<PagedResult<Cereal>> GetFilteredAsync(CerealFilter filter)
    {
        var query = db.Cereals.AsNoTracking().AsQueryable();

        // Name
        if (!string.IsNullOrEmpty(filter.NameContains))
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

        // Sorting — SortOrder is only applied when SortBy is also set;
        // Id is always appended as a tiebreaker for fully deterministic pagination.
        bool desc = filter.SortOrder == SortOrder.Desc;
        IOrderedQueryable<Cereal> ordered = filter.SortBy switch
        {
            SortBy.Name     => desc ? query.OrderByDescending(c => c.Name)     : query.OrderBy(c => c.Name),
            SortBy.Calories => desc ? query.OrderByDescending(c => c.Calories) : query.OrderBy(c => c.Calories),
            SortBy.Protein  => desc ? query.OrderByDescending(c => c.Protein)  : query.OrderBy(c => c.Protein),
            SortBy.Fat      => desc ? query.OrderByDescending(c => c.Fat)      : query.OrderBy(c => c.Fat),
            SortBy.Sodium   => desc ? query.OrderByDescending(c => c.Sodium)   : query.OrderBy(c => c.Sodium),
            SortBy.Fiber    => desc ? query.OrderByDescending(c => c.Fiber)    : query.OrderBy(c => c.Fiber),
            SortBy.Carbo    => desc ? query.OrderByDescending(c => c.Carbo)    : query.OrderBy(c => c.Carbo),
            SortBy.Sugars   => desc ? query.OrderByDescending(c => c.Sugars)   : query.OrderBy(c => c.Sugars),
            SortBy.Potass   => desc ? query.OrderByDescending(c => c.Potass)   : query.OrderBy(c => c.Potass),
            SortBy.Vitamins => desc ? query.OrderByDescending(c => c.Vitamins) : query.OrderBy(c => c.Vitamins),
            SortBy.Shelf    => desc ? query.OrderByDescending(c => c.Shelf)    : query.OrderBy(c => c.Shelf),
            SortBy.Weight   => desc ? query.OrderByDescending(c => c.Weight)   : query.OrderBy(c => c.Weight),
            SortBy.Cups     => desc ? query.OrderByDescending(c => c.Cups)     : query.OrderBy(c => c.Cups),
            SortBy.Rating   => desc ? query.OrderByDescending(c => c.Rating)   : query.OrderBy(c => c.Rating),
            _               => query.OrderBy(c => c.Name)
        };
        query = ordered.ThenBy(c => c.Id);

        int page     = Math.Max(1, filter.Page ?? 1);
        int pageSize = Math.Clamp(filter.PageSize ?? 20, 1, 100);

        var totalCount = await query.CountAsync();
        var items      = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Cereal>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

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

    public async Task<ImportResult> ImportAsync(Stream content, ImportFormat format)
    {
        var parsed = await CerealImportParser.ParseAsync(content, format);

        if (parsed.Count == 0)
            return new ImportResult(0, 0, []);

        // Pre-load any cereals that already exist for those names — one round-trip
        var validNames = parsed
            .Where(p => p.Row is not null && !string.IsNullOrWhiteSpace(p.Row.Name))
            .Select(p => p.Row!.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingByName = await db.Cereals
            .Where(c => validNames.Contains(c.Name))
            .ToDictionaryAsync(c => c.Name, StringComparer.OrdinalIgnoreCase);

        int inserted = 0, updated = 0;
        var skipped = new List<SkippedRow>();
        // Track entities added in this batch so duplicate names in the file
        // update the same in-memory entity rather than producing two DB rows.
        var addedThisBatch = new Dictionary<string, Cereal>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < parsed.Count; i++)
        {
            var (row, error) = parsed[i];
            int rowNumber = i + 1;

            if (error is not null)
            {
                skipped.Add(new SkippedRow(rowNumber, error));
                continue;
            }

            if (row is null)
            {
                skipped.Add(new SkippedRow(rowNumber, "Row is null."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                skipped.Add(new SkippedRow(rowNumber, "Name is required."));
                continue;
            }

            if (existingByName.TryGetValue(row.Name, out var existing))
            {
                ApplyImportRow(row, existing);
                updated++;
            }
            else if (addedThisBatch.TryGetValue(row.Name, out var inFlight))
            {
                // Duplicate name within the same file — last row wins, don't recount
                ApplyImportRow(row, inFlight);
            }
            else
            {
                var cereal = new Cereal();
                ApplyImportRow(row, cereal);
                db.Cereals.Add(cereal);
                addedThisBatch[row.Name] = cereal;
                inserted++;
            }
        }

        await db.SaveChangesAsync();
        return new ImportResult(inserted, updated, skipped);
    }

    private static void ApplyImportRow(CerealImportRow row, Cereal target)
    {
        target.Name     = row.Name!;
        target.Mfr      = row.Mfr;
        target.Type     = row.Type;
        target.Calories = row.Calories;
        target.Protein  = row.Protein;
        target.Fat      = row.Fat;
        target.Sodium   = row.Sodium;
        target.Fiber    = row.Fiber;
        target.Carbo    = row.Carbo;
        target.Sugars   = row.Sugars;
        target.Potass   = row.Potass;
        target.Vitamins = row.Vitamins;
        target.Shelf    = row.Shelf;
        target.Weight   = row.Weight;
        target.Cups     = row.Cups;
        target.Rating   = row.Rating;
    }

}
