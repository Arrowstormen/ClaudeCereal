using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
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

    // ── JSON ─────────────────────────────────────────────────────────────────────

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

    // ── CSV ──────────────────────────────────────────────────────────────────────

    private static List<(CerealImportRow? Row, string? Error)> ParseCsv(Stream content)
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter      = true,  // handles comma and semicolon automatically
            HeaderValidated      = null,  // don't throw on unrecognised columns
            MissingFieldFound    = null,  // don't throw on short rows
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
        });

        // Case-insensitive enum parsing for Manufacturer and CerealType
        csv.Context.TypeConverterOptionsCache.GetOptions<Manufacturer?>().EnumIgnoreCase = true;
        csv.Context.TypeConverterOptionsCache.GetOptions<CerealType?>().EnumIgnoreCase = true;

        if (!csv.Read()) return []; // empty file
        csv.ReadHeader();

        if (!csv.HeaderRecord?.Any(h => h.Equals("name", StringComparison.OrdinalIgnoreCase)) ?? true)
            throw new InvalidDataException("CSV is missing a required 'name' column.");

        var result = new List<(CerealImportRow?, string?)>();
        while (csv.Read())
        {
            try
            {
                result.Add((csv.GetRecord<CerealImportRow>(), null));
            }
            catch (CsvHelperException ex)
            {
                result.Add((null, ex.InnerException?.Message ?? ex.Message));
            }
        }
        return result;
    }
}
