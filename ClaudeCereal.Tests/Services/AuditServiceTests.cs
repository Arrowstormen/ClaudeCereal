using ClaudeCereal.Models;
using ClaudeCereal.Services;
using ClaudeCereal.Tests.Helpers;

namespace ClaudeCereal.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private AuditService NewService() => new(_factory.CreateContext());

    private async Task SeedAsync(IEnumerable<AuditLog> logs)
    {
        await using var db = _factory.CreateContext();
        db.AuditLogs.AddRange(logs);
        await db.SaveChangesAsync();
    }

    private static AuditLog MakeLog(
        int       entityId      = 1,
        string    actor         = "system",
        string    correlationId = "corr-1",
        AuditAction action      = AuditAction.Created,
        DateTime? timestamp     = null) => new()
    {
        EntityId      = entityId,
        Actor         = actor,
        CorrelationId = correlationId,
        Action        = action,
        EntityName    = "Cereal",
        Timestamp     = timestamp ?? DateTime.UtcNow,
        Changes       = []
    };

    // ── No filter ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsAllLogs_WithNoFilter()
    {
        await SeedAsync([MakeLog(entityId: 1101), MakeLog(entityId: 1102)]);

        var result = await NewService().GetPagedAsync(new AuditFilter());

        // Other tests share the same database, so TotalCount may be higher than 2.
        Assert.True(result.TotalCount >= 2);
        Assert.Contains(result.Items, l => l.EntityId == 1101);
        Assert.Contains(result.Items, l => l.EntityId == 1102);
    }

    // ── EntityId filter ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltersOnEntityId()
    {
        await SeedAsync([
            MakeLog(entityId: 201, actor: "alice"),
            MakeLog(entityId: 202, actor: "bob")
        ]);

        var result = await NewService().GetPagedAsync(new AuditFilter(EntityId: 201));

        Assert.All(result.Items, l => Assert.Equal(201, l.EntityId));
    }

    // ── Action filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltersOnAction()
    {
        await SeedAsync([
            MakeLog(entityId: 301, action: AuditAction.Created),
            MakeLog(entityId: 302, action: AuditAction.SoftDeleted)
        ]);

        var result = await NewService().GetPagedAsync(
            new AuditFilter(Action: AuditAction.Created));

        Assert.All(result.Items, l => Assert.Equal(AuditAction.Created, l.Action));
    }

    // ── Actor filter ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltersOnActor()
    {
        const string uniqueActor = "actor-unique-xyz";
        await SeedAsync([
            MakeLog(entityId: 401, actor: uniqueActor),
            MakeLog(entityId: 402, actor: "someone-else")
        ]);

        var result = await NewService().GetPagedAsync(new AuditFilter(Actor: uniqueActor));

        Assert.Single(result.Items);
        Assert.Equal(uniqueActor, result.Items[0].Actor);
    }

    // ── CorrelationId filter ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltersOnCorrelationId()
    {
        const string corrId = "corr-unique-abc";
        await SeedAsync([
            MakeLog(entityId: 501, correlationId: corrId),
            MakeLog(entityId: 502, correlationId: "other-corr")
        ]);

        var result = await NewService().GetPagedAsync(new AuditFilter(CorrelationId: corrId));

        Assert.Single(result.Items);
        Assert.Equal(corrId, result.Items[0].CorrelationId);
    }

    // ── Date range filter ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltersOnFromTimestamp()
    {
        var cutoff = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedAsync([
            MakeLog(entityId: 601, timestamp: cutoff.AddDays(-1)),
            MakeLog(entityId: 602, timestamp: cutoff.AddDays(+1))
        ]);

        var result = await NewService().GetPagedAsync(new AuditFilter(From: cutoff));

        Assert.All(result.Items, l => Assert.True(l.Timestamp >= cutoff));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersOnToTimestamp()
    {
        var cutoff = new DateTime(2030, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedAsync([
            MakeLog(entityId: 701, timestamp: cutoff.AddDays(-1)),
            MakeLog(entityId: 702, timestamp: cutoff.AddDays(+1))
        ]);

        var result = await NewService().GetPagedAsync(new AuditFilter(To: cutoff));

        Assert.All(result.Items, l => Assert.True(l.Timestamp <= cutoff));
    }

    // ── Sort order ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsMostRecentFirst()
    {
        var older = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedAsync([
            MakeLog(entityId: 801, timestamp: older),
            MakeLog(entityId: 802, timestamp: newer)
        ]);

        var result = await NewService().GetPagedAsync(
            new AuditFilter(From: older.AddSeconds(-1), To: newer.AddSeconds(+1)));

        // Items should be in descending timestamp order
        var timestamps = result.Items.Select(l => l.Timestamp).ToList();
        Assert.Equal(timestamps.OrderByDescending(t => t).ToList(), timestamps);
    }

    // ── Pagination ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_RespectsPageSize()
    {
        await SeedAsync(Enumerable.Range(901, 5).Select(i => MakeLog(entityId: i)));

        var result = await NewService().GetPagedAsync(
            new AuditFilter(Page: 1, PageSize: 2));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsDistinctItemsOnPage2()
    {
        const string actor = "page-nav-actor";
        await SeedAsync(Enumerable.Range(1001, 5).Select(i => MakeLog(entityId: i, actor: actor)));

        var page2 = await NewService().GetPagedAsync(
            new AuditFilter(Actor: actor, Page: 2, PageSize: 2));

        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(2, page2.Page);
        Assert.Equal(5, page2.TotalCount);
    }
}
