using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeCereal.Models;

namespace ClaudeCereal.Import;

internal static class CerealImportParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<List<(CerealImportRow? Row, string? Error)>> ParseAsync(
        Stream content, ImportFormat format) =>
        format == ImportFormat.Json
            ? ParseJsonAsync(content)
            : Task.FromResult(ParseCsv(content));

    private static async Task<List<(CerealImportRow? Row, string? Error)>> ParseJsonAsync(Stream content)
    {
        List<CerealImportRow?>? rows;
        try
        {
            rows = await JsonSerializer.DeserializeAsync<List<CerealImportRow?>>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON: {ex.Message}", ex);
        }

        return rows?.Select(r => (r, (string?)null)).ToList() ?? [];
    }

    private static List<(CerealImportRow? Row, string? Error)> ParseCsv(Stream content)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);

        if (lines.Count == 0) return [];

        // Auto-detect delimiter: whichever produces more columns from the header row wins
        var commaFields     = SplitLine(lines[0], ',');
        var semicolonFields = SplitLine(lines[0], ';');
        char delim          = commaFields.Length >= semicolonFields.Length ? ',' : ';';
        var headerFields    = delim == ',' ? commaFields : semicolonFields;

        // Build header → column-index map (case-insensitive, first occurrence wins)
        var headers = headerFields
            .Select((h, i) => (Name: h.Trim().ToLowerInvariant(), Index: i))
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.First().Index);

        if (!headers.ContainsKey("name"))
            throw new InvalidDataException("CSV is missing a required 'name' column.");

        var result = new List<(CerealImportRow?, string?)>();

        for (int i = 1; i < lines.Count; i++)
        {
            var fields = SplitLine(lines[i], delim);

            string Get(string header) =>
                headers.TryGetValue(header, out int idx) && idx < fields.Length
                    ? fields[idx].Trim()
                    : string.Empty;

            var row = new CerealImportRow
            {
                Name     = NullIfEmpty(Get("name")),
                Mfr      = ParseEnum<Manufacturer>(Get("mfr")),
                Type     = ParseEnum<CerealType>(Get("type")),
                Calories = ParseInt(Get("calories")),
                Protein  = ParseInt(Get("protein")),
                Fat      = ParseInt(Get("fat")),
                Sodium   = ParseInt(Get("sodium")),
                Fiber    = ParseDouble(Get("fiber")),
                Carbo    = ParseDouble(Get("carbo")),
                Sugars   = ParseInt(Get("sugars")),
                Potass   = ParseInt(Get("potass")),
                Vitamins = ParseInt(Get("vitamins")),
                Shelf    = ParseInt(Get("shelf")),
                Weight   = ParseDouble(Get("weight")),
                Cups     = ParseDouble(Get("cups")),
                Rating   = ParseDouble(Get("rating")),
            };

            result.Add((row, null));
        }

        return result;
    }

    /// <summary>
    /// Splits a single CSV line respecting RFC 4180 quoting rules:
    /// fields may be wrapped in double-quotes, and a literal double-quote
    /// inside a quoted field is represented as two consecutive double-quotes ("").
    /// </summary>
    private static string[] SplitLine(string line, char delim)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"'); // escaped quote inside a quoted field
                        i++;            // skip the second quote
                    }
                    else
                    {
                        inQuotes = false; // closing quote
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delim)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString()); // last field (no trailing delimiter)
        return [.. fields];
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    private static T? ParseEnum<T>(string s) where T : struct, Enum =>
        Enum.TryParse<T>(s, ignoreCase: true, out var result) ? result : null;

    private static int? ParseInt(string s) =>
        int.TryParse(s, out var v) ? v : null;

    private static double? ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}
