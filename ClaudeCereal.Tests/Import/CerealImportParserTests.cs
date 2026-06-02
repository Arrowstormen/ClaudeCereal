using System.Text;
using ClaudeCereal.Import;
using ClaudeCereal.Models;

namespace ClaudeCereal.Tests.Import;

public class CerealImportParserTests
{
    private static Stream ToStream(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));

    // ── CSV ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_WhenCsvHasValidRows_ShouldReturnAllParsedRows()
    {
        const string csv = "name,calories\r\nCheerios,110\r\nFroot Loops,130\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        Assert.Equal(2, rows.Count);
        var first = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal("Cheerios", first.Row.Name);
        Assert.Equal(110, first.Row.Calories);

        var second = Assert.IsType<ParsedRow.Ok>(rows[1]);
        Assert.Equal("Froot Loops", second.Row.Name);
        Assert.Equal(130, second.Row.Calories);
    }

    [Fact]
    public async Task ParseAsync_WhenCsvStreamIsEmpty_ShouldReturnEmptyList()
    {
        var rows = await CerealImportParser.ParseAsync(ToStream(""), ImportFormat.Csv);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task ParseAsync_WhenCsvIsMissingNameColumn_ShouldThrowInvalidDataException()
    {
        const string csv = "calories,protein\r\n110,3\r\n";

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv));
    }

    [Fact]
    public async Task ParseAsync_WhenCsvEnumValuesAreMixedCase_ShouldParseThem()
    {
        const string csv = "name,mfr,type\r\nTest Cereal,G,H\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal(Manufacturer.G, ok.Row.Mfr);
        Assert.Equal(CerealType.H, ok.Row.Type);
    }

    [Fact]
    public async Task ParseAsync_WhenCsvRowHasMissingOptionalFields_ShouldSetThemToNull()
    {
        // Row has only a name column — all nutrition fields should be null
        const string csv = "name\r\nMin Fields Cereal\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal("Min Fields Cereal", ok.Row.Name);
        Assert.Null(ok.Row.Calories);
        Assert.Null(ok.Row.Protein);
    }

    [Fact]
    public async Task ParseAsync_WhenCsvHeadersAreUpperCase_ShouldStillParseCorrectly()
    {
        const string csv = "NAME,CALORIES\r\nCase Insensitive,90\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal("Case Insensitive", ok.Row.Name);
        Assert.Equal(90, ok.Row.Calories);
    }

    [Fact]
    public async Task ParseAsync_WhenCsvRowHasInvalidEnumValue_ShouldReturnErrRow()
    {
        const string csv = "name,mfr\r\nBad Enum Cereal,INVALID_MFR\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        Assert.Single(rows);
        Assert.IsType<ParsedRow.Err>(rows[0]);
    }

    // ── JSON ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_WhenJsonArrayHasValidObjects_ShouldReturnAllParsedRows()
    {
        const string json = """
            [
                {"name": "Corn Flakes", "calories": 100},
                {"name": "Wheaties",    "calories": 100}
            ]
            """;

        var rows = await CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json);

        Assert.Equal(2, rows.Count);
        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal("Corn Flakes", ok.Row.Name);
    }

    [Fact]
    public async Task ParseAsync_WhenJsonArrayIsEmpty_ShouldReturnEmptyList()
    {
        var rows = await CerealImportParser.ParseAsync(ToStream("[]"), ImportFormat.Json);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task ParseAsync_WhenJsonArrayContainsNullEntry_ShouldReturnErrRow()
    {
        const string json = """[{"name":"Valid"},null]""";

        var rows = await CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json);

        Assert.Equal(2, rows.Count);
        Assert.IsType<ParsedRow.Ok>(rows[0]);
        var err = Assert.IsType<ParsedRow.Err>(rows[1]);
        Assert.Equal("Row is null.", err.Error);
    }

    [Fact]
    public async Task ParseAsync_WhenJsonIsMalformed_ShouldThrowInvalidDataException()
    {
        const string json = "{ definitely not json }";

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json));
    }

    [Fact]
    public async Task ParseAsync_WhenJsonContainsEnumFields_ShouldParseThem()
    {
        const string json = """[{"name":"Enum Test","mfr":"G","type":"H"}]""";

        var rows = await CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal(Manufacturer.G, ok.Row.Mfr);
        Assert.Equal(CerealType.H, ok.Row.Type);
    }

    [Fact]
    public async Task ParseAsync_WhenJsonContainsInvalidEnumValue_ShouldThrowInvalidDataException()
    {
        // JsonStringEnumConverter rejects unknown values — the whole parse fails
        const string json = """[{"name":"Bad Enum","mfr":"INVALID_MFR"}]""";

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json));
    }
}
