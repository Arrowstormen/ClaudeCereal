using ClaudeCereal.Extensions;
using ClaudeCereal.Models;
using ClaudeCereal.Tests.Helpers;

namespace ClaudeCereal.Tests.Extensions;

public class PaginationExtensionsTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task SeedCerealsAsync(params string[] names)
    {
        await using var db = _factory.CreateContext();
        foreach (var name in names)
            db.Cereals.Add(new Cereal { Name = name });
        await db.SaveChangesAsync();
    }

    // ── Page normalisation ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToPagedResultAsync_WhenPageIsNull_ShouldDefaultToPage1()
    {
        await SeedCerealsAsync("Pagination Default 1", "Pagination Default 2");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name.StartsWith("Pagination Default"))
            .ToPagedResultAsync(page: null, pageSize: 10);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task ToPagedResultAsync_WhenPageIsNegative_ShouldClampToOne()
    {
        await SeedCerealsAsync("Clamp Page Test");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name == "Clamp Page Test")
            .ToPagedResultAsync(page: -5, pageSize: 10);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task ToPagedResultAsync_WhenPageSizeIsNull_ShouldUseDefaultPageSize()
    {
        await SeedCerealsAsync("Default PS Test");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name == "Default PS Test")
            .ToPagedResultAsync(page: 1, pageSize: null);

        Assert.Equal(PaginationExtensions.DefaultPageSize, result.PageSize);
    }

    [Fact]
    public async Task ToPagedResultAsync_WhenPageSizeExceedsMax_ShouldClampToMax()
    {
        await SeedCerealsAsync("MaxPageSize Test");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name == "MaxPageSize Test")
            .ToPagedResultAsync(page: 1, pageSize: 9999);

        Assert.Equal(PaginationExtensions.MaxPageSize, result.PageSize);
    }

    // ── Correct slicing ────────────────────────────────────────────────────────

    [Fact]
    public async Task ToPagedResultAsync_WhenOnPage2_ShouldReturnCorrectSlice()
    {
        await SeedCerealsAsync("Slice Test 01", "Slice Test 02", "Slice Test 03", "Slice Test 04");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name.StartsWith("Slice Test"))
            .OrderBy(c => c.Name)
            .ToPagedResultAsync(page: 2, pageSize: 2);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Slice Test 03", result.Items[0].Name);
        Assert.Equal("Slice Test 04", result.Items[1].Name);
    }

    [Fact]
    public async Task ToPagedResultAsync_WhenPageBeyondTotal_ShouldReturnEmptyItems()
    {
        await SeedCerealsAsync("BeyondPage Only");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name == "BeyondPage Only")
            .ToPagedResultAsync(page: 99, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    // ── TotalPages calculation ─────────────────────────────────────────────────

    [Fact]
    public async Task ToPagedResultAsync_WhenMultiplePages_ShouldComputeTotalPagesCorrectly()
    {
        await SeedCerealsAsync("TotalPages 1", "TotalPages 2", "TotalPages 3", "TotalPages 4", "TotalPages 5");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name.StartsWith("TotalPages"))
            .ToPagedResultAsync(page: 1, pageSize: 2);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages); // ceil(5 / 2) = 3
    }

    [Fact]
    public async Task ToPagedResultAsync_WhenResultFitsOnSinglePage_ShouldSetTotalPagesToOne()
    {
        await SeedCerealsAsync("SinglePage Only");

        await using var db = _factory.CreateContext();
        var result = await db.Cereals
            .Where(c => c.Name == "SinglePage Only")
            .ToPagedResultAsync(page: 1, pageSize: 10);

        Assert.Equal(1, result.TotalPages);
    }
}
