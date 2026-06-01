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

    public static Task<List<ParsedRow>> ParseAsync(
        Stream content, ImportFormat format, CancellationToken cancellationToken = default) =>
        format == ImportFormat.Json
            ? ParseJsonAsync(content, cancellationToken)
            : Task.FromResult(ParseCsv(content, cancellationToken));

    // ── JSON ─────────────────────────────────────────────────────────────────────

    private static async Task<List<ParsedRow>> ParseJsonAsync(
        Stream content, CancellationToken cancellationToken = default)
    {
        List<CerealImportRow?>? rows;
        try
        {
            rows = await JsonSerializer.DeserializeAsync<List<CerealImportRow?>>(
                content, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid JSON: {ex.Message}", ex);
        }

        return rows?.Select(r => r is null
            ? (ParsedRow)new ParsedRow.Err("Row is null.")
            : new ParsedRow.Ok(r))
            .ToList() ?? [];
    }

    // ── CSV ──────────────────────────────────────────────────────────────────────

    private static List<ParsedRow> ParseCsv(Stream content, CancellationToken cancellationToken = default)
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

        var result = new List<ParsedRow>();
        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                result.Add(new ParsedRow.Ok(csv.GetRecord<CerealImportRow>()));
            }
            catch (CsvHelperException ex)
            {
                result.Add(new ParsedRow.Err(ex.InnerException?.Message ?? ex.Message));
            }
        }
        return result;
    }
}
