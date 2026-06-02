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
        var fields = line.Split(';');

        return new Cereal
        {
            Name     = fields[0].Trim(),
            Mfr      = ParseEnum<Manufacturer>(fields[1]),
            Type     = ParseEnum<CerealType>(fields[2]),
            Calories = ParseInt(fields[3]),
            Protein  = ParseInt(fields[4]),
            Fat      = ParseInt(fields[5]),
            Sodium   = ParseInt(fields[6]),
            Fiber    = ParseDouble(fields[7]),
            Carbo    = ParseDouble(fields[8]),
            Sugars   = ParseInt(fields[9]),
            Potass   = ParseInt(fields[10]),
            Vitamins = ParseInt(fields[11]),
            Shelf    = ParseInt(fields[12]),
            Weight   = ParseDouble(fields[13]),
            Cups     = ParseDouble(fields[14]),
            Rating   = ParseDouble(fields[15]),
        };
    }

    private static T? ParseEnum<T>(string input) where T : struct, Enum =>
        Enum.TryParse<T>(input.Trim(), ignoreCase: true, out var result) ? result : null;

    private static int? ParseInt(string input)
    {
        if (int.TryParse(input.Trim(), out var parsed) && parsed != -1)
            return parsed;
        return null;
    }

    private static double? ParseDouble(string input)
    {
        if (double.TryParse(input.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed != -1)
            return parsed;
        return null;
    }
}
