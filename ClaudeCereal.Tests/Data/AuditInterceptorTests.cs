using System.Security.Claims;
using ClaudeCereal.Data;
using ClaudeCereal.Models;
using ClaudeCereal.Services;
using ClaudeCereal.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClaudeCereal.Tests.Data;

public class AuditInterceptorTests : IDisposable
{
    private readonly SqliteDbContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // Creates an AuditInterceptor with a fake HttpContext for the given actor and a fixed correlation ID.
    private static AuditInterceptor MakeInterceptor(string actor = "test-actor")
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, actor)], "Test");
        var principal = new ClaimsPrincipal(identity);

        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(c => c.User).Returns(principal);
        mockHttpContext.Setup(c => c.TraceIdentifier).Returns("test-corr-id");

        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

        return new AuditInterceptor(mockAccessor.Object);
    }

    // ── Created ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenInsert_ShouldCreateAuditLog()
    {
        // Arrange
        var interceptor = MakeInterceptor("alice");
        await using var db = _factory.CreateContextWithInterceptor(interceptor);

        // Act
        var cereal = await new CerealService(db)
            .CreateAsync(new CerealRequest { Name = "Interceptor Create Test" });

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.Created);
        Assert.NotNull(log);
        Assert.Equal("alice", log.Actor);
        Assert.Equal("Interceptor Create Test", log.EntityName);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCreate_ShouldCaptureFieldChanges()
    {
        // Arrange
        var interceptor = MakeInterceptor();
        await using var db = _factory.CreateContextWithInterceptor(interceptor);

        // Act
        var cereal = await new CerealService(db)
            .CreateAsync(new CerealRequest { Name = "Field Changes Test", Calories = 120 });

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.Created);
        Assert.NotNull(log);
        Assert.Contains(log.Changes, c => c.Field == "Name"     && c.NewValue == "Field Changes Test");
        Assert.Contains(log.Changes, c => c.Field == "Calories" && c.NewValue == "120");
    }

    // ── Updated ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenUpdate_ShouldCreateAuditLog()
    {
        // Arrange — create without interceptor so no spurious Created log is written
        await using var setupDb = _factory.CreateContext();
        var cereal = new Cereal { Name = "Interceptor Update Test" };
        setupDb.Cereals.Add(cereal);
        await setupDb.SaveChangesAsync();

        // Act
        var interceptor = MakeInterceptor("bob");
        await using var updateDb = _factory.CreateContextWithInterceptor(interceptor);
        await new CerealService(updateDb)
            .UpdateAsync(cereal.Id, new CerealRequest { Name = "Interceptor Update Test Renamed", Version = cereal.Version });

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.Updated);
        Assert.NotNull(log);
        Assert.Equal("bob", log.Actor);
        Assert.Contains(log.Changes, c => c.Field == "Name");
    }

    // ── SoftDeleted ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenSoftDelete_ShouldCreateAuditLog()
    {
        // Arrange — create without interceptor so no spurious Created log is written
        await using var setupDb = _factory.CreateContext();
        var cereal = new Cereal { Name = "Interceptor Delete Test" };
        setupDb.Cereals.Add(cereal);
        await setupDb.SaveChangesAsync();

        // Act
        var interceptor = MakeInterceptor();
        await using var deleteDb = _factory.CreateContextWithInterceptor(interceptor);
        await new CerealService(deleteDb).DeleteAsync(cereal.Id);

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.SoftDeleted);
        Assert.NotNull(log);
        Assert.Empty(log.Changes); // field changes are not captured for soft-deletes
    }

    // ── Restored ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenRestore_ShouldCreateAuditLog()
    {
        // Arrange — create an already-deleted row without interceptor
        await using var setupDb = _factory.CreateContext();
        var cereal = new Cereal { Name = "Interceptor Restore Test", DeletedAt = DateTime.UtcNow };
        setupDb.Cereals.Add(cereal);
        await setupDb.SaveChangesAsync();

        // Act — IgnoreQueryFilters needed to find the soft-deleted row
        var interceptor = MakeInterceptor();
        await using var restoreDb = _factory.CreateContextWithInterceptor(interceptor);
        var found = await restoreDb.Cereals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cereal.Id);
        Assert.NotNull(found);
        found.DeletedAt = null;
        await restoreDb.SaveChangesAsync();

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.Restored);
        Assert.NotNull(log);
    }

    // ── Actor / CorrelationId propagation ──────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenHttpContextIsNull_ShouldUseSystemActor()
    {
        // Arrange
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        await using var db = _factory.CreateContextWithInterceptor(new AuditInterceptor(mockAccessor.Object));

        // Act
        var cereal = await new CerealService(db)
            .CreateAsync(new CerealRequest { Name = "System Actor Test" });

        // Assert
        await using var readDb = _factory.CreateContext();
        var log = await readDb.AuditLogs
            .FirstOrDefaultAsync(l => l.EntityId == cereal.Id && l.Action == AuditAction.Created);
        Assert.NotNull(log);
        Assert.Equal("system", log.Actor);
    }

    // ── Tamper protection ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WhenAuditLogIsModified_ShouldThrow()
    {
        // Arrange
        await using var setupDb = _factory.CreateContext();
        var cereal = new Cereal { Name = "Tamper Target" };
        setupDb.Cereals.Add(cereal);
        setupDb.AuditLogs.Add(new AuditLog
        {
            EntityId = 0, Actor = "seeder", CorrelationId = "c1",
            Action = AuditAction.Created, EntityName = "Test", Timestamp = DateTime.UtcNow,
            Changes = []
        });
        await setupDb.SaveChangesAsync();

        // Act & Assert
        var interceptor = MakeInterceptor();
        await using var tamperDb = _factory.CreateContextWithInterceptor(interceptor);
        var log = await tamperDb.AuditLogs.FirstAsync();
        log.Actor = "hacker";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => tamperDb.SaveChangesAsync());
    }
}
