using ClaudeCereal.Data;
using ClaudeCereal.Import;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Services;

public class CerealImportService(AppDbContext db) : ICerealImportService
{
    public async Task<ImportResult> ImportAsync(
        Stream content, ImportFormat format, CancellationToken cancellationToken = default)
    {
        var parsed = await CerealImportParser.ParseAsync(content, format, cancellationToken);

        if (parsed.Count == 0)
            return new ImportResult(0, 0, []);

        // Pre-load any cereals that already exist for those names — one round-trip.
        // Global query filter means only active (non-deleted) rows are matched here;
        // restoring soft-deleted rows via import is tracked as a future improvement.
        var validNames = parsed
            .OfType<ParsedRow.Ok>()
            .Select(p => p.Row.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OfType<string>()                          // narrows string? to string
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingByName = await db.Cereals
            .Where(c => validNames.Contains(c.Name))
            .ToDictionaryAsync(c => c.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        int inserted = 0;
        int updated  = 0;
        var skipped = new List<SkippedRow>();
        // Track entities added in this batch so duplicate names in the file
        // update the same in-memory entity rather than producing two DB rows.
        var addedThisBatch = new Dictionary<string, Cereal>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < parsed.Count; i++)
        {
            int rowNumber = i + 1;

            switch (parsed[i])
            {
                case ParsedRow.Err { Error: var error }:
                    skipped.Add(new SkippedRow(rowNumber, error));
                    continue;

                // `is { } name` matches non-null and binds as string (non-nullable),
                // so name can be passed directly to dictionary lookups without warnings.
                case ParsedRow.Ok ok when ok.Row.Name is { } name && !string.IsNullOrWhiteSpace(name):
                    if (existingByName.TryGetValue(name, out var existing))
                    {
                        ApplyImportRow(ok.Row, existing);
                        updated++;
                    }
                    else if (addedThisBatch.TryGetValue(name, out var inFlight))
                    {
                        // Duplicate name within the same file — last row wins, don't recount
                        ApplyImportRow(ok.Row, inFlight);
                    }
                    else
                    {
                        var cereal = new Cereal();
                        ApplyImportRow(ok.Row, cereal);
                        db.Cereals.Add(cereal);
                        addedThisBatch[name] = cereal;
                        inserted++;
                    }
                    break;

                case ParsedRow.Ok:
                    // Null or whitespace name — previous case's when guard didn't match
                    skipped.Add(new SkippedRow(rowNumber, "Name is required."));
                    continue;
            }
        }

        await db.SaveChangesAsync(CancellationToken.None);
        return new ImportResult(inserted, updated, skipped);
    }

    private static void ApplyImportRow(CerealImportRow row, Cereal target)
    {
        target.Name     = row.Name ?? throw new ArgumentException("Name must not be null.", nameof(row));
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
