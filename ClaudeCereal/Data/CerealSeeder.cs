using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public class CerealSeeder(ILogger<CerealSeeder> logger)
{
    public async Task SeedAsync(AppDbContext db, string csvPath)
    {
        await db.Database.MigrateAsync();

        if (await db.Cereals.AnyAsync())
            return;

        if (!File.Exists(csvPath))
        {
            logger.LogError("Seed file not found at {Path}. Database will not be seeded.", csvPath);
            return;
        }

        var lines = await File.ReadAllLinesAsync(csvPath);

        // line 0 = header, line 1 = type annotations — skip both
        var cereals = lines.Skip(2)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select((line, index) => TryParseLine(line, index + 3))
            .OfType<Cereal>()
            .ToList();

        db.Cereals.AddRange(cereals);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} cereals from {Path}.", cereals.Count, csvPath);
    }

    private Cereal? TryParseLine(string line, int lineNumber)
    {
        var f = line.Split(';');

        if (f.Length < 16)
        {
            logger.LogWarning("CSV line {LineNumber} skipped: expected 16 fields, got {Count}.", lineNumber, f.Length);
            return null;
        }

        return new Cereal
        {
            Name     = f[0].Trim(),
            Mfr      = ParseEnum<Manufacturer>(f[1]),
            Type     = ParseEnum<CerealType>(f[2]),
            Calories = ParseInt(f[3]),
            Protein  = ParseInt(f[4]),
            Fat      = ParseInt(f[5]),
            Sodium   = ParseInt(f[6]),
            Fiber    = ParseDouble(f[7]),
            Carbo    = ParseDouble(f[8]),
            Sugars   = ParseInt(f[9]),
            Potass   = ParseInt(f[10]),
            Vitamins = ParseInt(f[11]),
            Shelf    = ParseInt(f[12]),
            Weight   = ParseDouble(f[13]),
            Cups     = ParseDouble(f[14]),
            Rating   = ParseDouble(f[15]),
        };
    }

    private static T? ParseEnum<T>(string s) where T : struct, Enum =>
        Enum.TryParse<T>(s.Trim(), ignoreCase: true, out var result) ? result : null;

    private static int? ParseInt(string s)
    {
        if (int.TryParse(s.Trim(), out var v) && v != -1)
            return v;
        return null;
    }

    private static double? ParseDouble(string s)
    {
        if (double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) && v != -1)
            return v;
        return null;
    }
}
