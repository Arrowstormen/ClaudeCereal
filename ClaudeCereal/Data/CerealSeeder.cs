using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Data;

public static class CerealSeeder
{
    public static async Task SeedAsync(AppDbContext db, string csvPath)
    {
        await db.Database.MigrateAsync();

        if (await db.Cereals.AnyAsync())
            return;

        var lines = await File.ReadAllLinesAsync(csvPath);

        // line 0 = header, line 1 = type annotations — skip both
        var cereals = lines.Skip(2)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(ParseLine)
            .ToList();

        db.Cereals.AddRange(cereals);
        await db.SaveChangesAsync();
    }

    private static Cereal ParseLine(string line)
    {
        var f = line.Split(';');

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
