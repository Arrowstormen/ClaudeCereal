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
    public async Task ParseAsync_Csv_ParsesAllRows()
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
    public async Task ParseAsync_Csv_ReturnsEmptyList_ForEmptyStream()
    {
        var rows = await CerealImportParser.ParseAsync(ToStream(""), ImportFormat.Csv);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task ParseAsync_Csv_Throws_WhenNameColumnIsMissing()
    {
        const string csv = "calories,protein\r\n110,3\r\n";

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv));
    }

    [Fact]
    public async Task ParseAsync_Csv_ParsesEnumFieldsCaseInsensitively()
    {
        const string csv = "name,mfr,type\r\nTest Cereal,G,H\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal(Manufacturer.G, ok.Row.Mfr);
        Assert.Equal(CerealType.H, ok.Row.Type);
    }

    [Fact]
    public async Task ParseAsync_Csv_ToleratesmissingFields_AreNullOnRow()
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
    public async Task ParseAsync_Csv_IsHeaderCaseInsensitive()
    {
        const string csv = "NAME,CALORIES\r\nCase Insensitive,90\r\n";

        var rows = await CerealImportParser.ParseAsync(ToStream(csv), ImportFormat.Csv);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal("Case Insensitive", ok.Row.Name);
        Assert.Equal(90, ok.Row.Calories);
    }

    // ── JSON ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseAsync_Json_ParsesAllRows()
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
    public async Task ParseAsync_Json_ReturnsEmptyList_ForEmptyArray()
    {
        var rows = await CerealImportParser.ParseAsync(ToStream("[]"), ImportFormat.Json);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task ParseAsync_Json_Returns_ErrRow_ForNullEntry()
    {
        const string json = """[{"name":"Valid"},null]""";

        var rows = await CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json);

        Assert.Equal(2, rows.Count);
        Assert.IsType<ParsedRow.Ok>(rows[0]);
        var err = Assert.IsType<ParsedRow.Err>(rows[1]);
        Assert.Equal("Row is null.", err.Error);
    }

    [Fact]
    public async Task ParseAsync_Json_Throws_OnMalformedJson()
    {
        const string json = "{ definitely not json }";

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json));
    }

    [Fact]
    public async Task ParseAsync_Json_ParsesEnumFields()
    {
        const string json = """[{"name":"Enum Test","mfr":"G","type":"H"}]""";

        var rows = await CerealImportParser.ParseAsync(ToStream(json), ImportFormat.Json);

        var ok = Assert.IsType<ParsedRow.Ok>(rows[0]);
        Assert.Equal(Manufacturer.G, ok.Row.Mfr);
        Assert.Equal(CerealType.H, ok.Row.Type);
    }
}
