using System.Text;
using ClaudeCereal.Exceptions;
using ClaudeCereal.Models;
using ClaudeCereal.Services;
using ClaudeCereal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Tests.Services;

public class CerealServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private CerealService NewService() => new(_factory.CreateContext());

    private static CerealRequest Request(string name, int calories = 110) => new()
    {
        Name     = name,
        Calories = calories,
        Protein  = 3
    };

    // ── GetByIdAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsCereal_WhenFound()
    {
        var created = await NewService().CreateAsync(Request("Cheerios Get"));

        var found = await NewService().GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Cheerios Get", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdNotFound()
    {
        var result = await NewService().GetByIdAsync(99999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForSoftDeletedCereal()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("Soft Get Test"));
        await svc.DeleteAsync(created.Id);

        var result = await NewService().GetByIdAsync(created.Id);

        Assert.Null(result); // global query filter hides soft-deleted rows
    }

    // ── IsDeletedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task IsDeletedAsync_ReturnsFalse_ForActiveRow()
    {
        var created = await NewService().CreateAsync(Request("IsDeleted Active"));

        Assert.False(await NewService().IsDeletedAsync(created.Id));
    }

    [Fact]
    public async Task IsDeletedAsync_ReturnsTrue_AfterSoftDelete()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("IsDeleted Deleted"));
        await svc.DeleteAsync(created.Id);

        Assert.True(await NewService().IsDeletedAsync(created.Id));
    }

    [Fact]
    public async Task IsDeletedAsync_ReturnsFalse_WhenIdNotFound()
    {
        Assert.False(await NewService().IsDeletedAsync(99999));
    }

    // ── CreateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AssignsId_AndPersistsCereal()
    {
        var cereal = await NewService().CreateAsync(Request("Rice Krispies Create"));

        Assert.True(cereal.Id > 0);
        Assert.Equal("Rice Krispies Create", cereal.Name);
        Assert.Equal(110, cereal.Calories);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameMatchesSoftDeletedRow()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("Conflict Cereal Create"));
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<SoftDeletedConflictException>(
            () => NewService().CreateAsync(Request("Conflict Cereal Create")));
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedCereal()
    {
        var created = await NewService().CreateAsync(Request("Update Original"));

        var req = Request("Update Renamed");
        req.Version = created.Version;
        var updated = await NewService().UpdateAsync(created.Id, req);

        Assert.NotNull(updated);
        Assert.Equal("Update Renamed", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_IncrementsVersion()
    {
        var created = await NewService().CreateAsync(Request("Version Bump"));
        var originalVersion = created.Version;

        var req = Request("Version Bump");
        req.Version = originalVersion;
        var updated = await NewService().UpdateAsync(created.Id, req);

        Assert.Equal(originalVersion + 1, updated!.Version);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        var result = await NewService().UpdateAsync(99999, Request("Ghost Update"));

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Throws_OnStaleVersion()
    {
        var created = await NewService().CreateAsync(Request("Stale Version"));

        var req = Request("Stale Version Updated");
        req.Version = 99; // stale — entity is at 0

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => NewService().UpdateAsync(created.Id, req));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndSoftDeletes()
    {
        var created = await NewService().CreateAsync(Request("Delete Me"));

        var deleted = await NewService().DeleteAsync(created.Id);

        Assert.True(deleted);

        // Row still exists; only DeletedAt is set
        await using var db = _factory.CreateContext();
        var row = await db.Cereals.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == created.Id);
        Assert.NotNull(row);
        Assert.NotNull(row.DeletedAt);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await NewService().DeleteAsync(99999));
    }

    // ── RestoreAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RestoreAsync_ClearsDeletedAt_AndReturnsCereal()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("Restore Me"));
        await svc.DeleteAsync(created.Id);

        var restored = await NewService().RestoreAsync(created.Id);

        Assert.NotNull(restored);
        Assert.Null(restored.DeletedAt);
    }

    [Fact]
    public async Task RestoreAsync_ReturnsNull_WhenIdNotFound()
    {
        Assert.Null(await NewService().RestoreAsync(99999));
    }

    [Fact]
    public async Task RestoreAsync_Throws_WhenCerealIsAlreadyActive()
    {
        var created = await NewService().CreateAsync(Request("Already Active Restore"));

        await Assert.ThrowsAsync<CerealAlreadyActiveException>(
            () => NewService().RestoreAsync(created.Id));
    }

    // ── GetFilteredAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetFilteredAsync_ReturnsOnlyActiveRows_ByDefault()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("Filter Soft Deleted Exclusion"));
        await svc.DeleteAsync(created.Id);

        var result = await NewService().GetFilteredAsync(
            new CerealFilter { NameContains = "Filter Soft Deleted Exclusion" });

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetFilteredAsync_IncludesSoftDeleted_WhenRequested()
    {
        var svc     = NewService();
        var created = await svc.CreateAsync(Request("Filter Include Deleted"));
        await svc.DeleteAsync(created.Id);

        var result = await NewService().GetFilteredAsync(
            new CerealFilter { NameContains = "Filter Include Deleted", IncludeDeleted = true });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersOnNameContains()
    {
        await NewService().CreateAsync(Request("NameFilter UniqueXYZ123"));
        await NewService().CreateAsync(Request("NameFilter OtherABC456"));

        var result = await NewService().GetFilteredAsync(
            new CerealFilter { NameContains = "UniqueXYZ123" });

        Assert.Single(result.Items);
        Assert.Equal("NameFilter UniqueXYZ123", result.Items[0].Name);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersOnCalorieRange()
    {
        await NewService().CreateAsync(new CerealRequest { Name = "LowCal FilterTest",  Calories = 50 });
        await NewService().CreateAsync(new CerealRequest { Name = "HighCal FilterTest", Calories = 200 });

        var result = await NewService().GetFilteredAsync(
            new CerealFilter { NameContains = "FilterTest", MinCalories = 100, MaxCalories = 250 });

        Assert.Single(result.Items);
        Assert.Equal("HighCal FilterTest", result.Items[0].Name);
    }

    [Fact]
    public async Task GetFilteredAsync_SortsByNameAscending_ByDefault()
    {
        await NewService().CreateAsync(Request("Sort Zucchini"));
        await NewService().CreateAsync(Request("Sort Apple"));

        var result = await NewService().GetFilteredAsync(
            new CerealFilter { NameContains = "Sort " });

        // Default sort is by Name ascending
        var names = result.Items.Select(c => c.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    // ── ImportAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_Csv_InsertsNewCereals()
    {
        const string csv = "name,calories\r\nImport New A,120\r\nImport New B,140\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await NewService().ImportAsync(stream, ImportFormat.Csv);

        Assert.Equal(2, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public async Task ImportAsync_Csv_UpdatesExistingCereals()
    {
        await NewService().CreateAsync(new CerealRequest { Name = "Import Update Existing", Calories = 100 });

        const string csv = "name,calories\r\nImport Update Existing,999\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await NewService().ImportAsync(stream, ImportFormat.Csv);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Updated);
    }

    [Fact]
    public async Task ImportAsync_Csv_SkipsRowsWithNullName()
    {
        // Parser requires "name" — use an empty value to exercise the blank-name skip path.
        const string csv = "name,calories\r\n,110\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await NewService().ImportAsync(stream, ImportFormat.Csv);

        Assert.Equal(0, result.Inserted);
        Assert.Single(result.Skipped);
    }

    [Fact]
    public async Task ImportAsync_Json_InsertsNewCereals()
    {
        const string json = """[{"name":"Import JSON Cereal","calories":120}]""";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var result = await NewService().ImportAsync(stream, ImportFormat.Json);

        Assert.Equal(1, result.Inserted);
    }
}
