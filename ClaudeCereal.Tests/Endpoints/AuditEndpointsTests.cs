using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ClaudeCereal.Authentication;
using ClaudeCereal.Models;
using ClaudeCereal.Tests.Helpers;
using Moq;

namespace ClaudeCereal.Tests.Endpoints;

public class AuditEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuditEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
    }

    // ── Authorization ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAudit_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateUnauthenticatedClient()
            .GetAsync("/audit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_AsReader_Returns403()
    {
        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_AsEditor_Returns403()
    {
        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .GetAsync("/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_AsAdmin_Returns200()
    {
        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 1, 20, 0, 0));

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .GetAsync("/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Response shape ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAudit_AsAdmin_ReturnsPagedResult()
    {
        var log = new AuditLog
        {
            Id = 1, EntityId = 10, Actor = "alice",
            Action = AuditAction.Created, EntityName = "Cereal",
            CorrelationId = "corr-1", Timestamp = DateTime.UtcNow,
            Changes = [new AuditFieldChange { Field = "Name", OldValue = null, NewValue = "Cheerios" }]
        };

        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([log], 1, 20, 1, 1));

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .GetAsync("/audit");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLog>>(TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("alice", result.Items[0].Actor);
        Assert.Equal(AuditAction.Created, result.Items[0].Action);
    }

    // ── Filter query parameters ────────────────────────────────────────────────

    [Fact]
    public async Task GetAudit_PassesEntityIdFilter_ToService()
    {
        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 1, 20, 0, 0));

        await _factory.CreateClientWithRole(Roles.Admin)
            .GetAsync("/audit?EntityId=42");

        _factory.AuditService.Verify(
            s => s.GetPagedAsync(
                It.Is<AuditFilter>(f => f.EntityId == 42),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAudit_PassesActionFilter_ToService()
    {
        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 1, 20, 0, 0));

        await _factory.CreateClientWithRole(Roles.Admin)
            .GetAsync("/audit?Action=SoftDeleted");

        _factory.AuditService.Verify(
            s => s.GetPagedAsync(
                It.Is<AuditFilter>(f => f.Action == AuditAction.SoftDeleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAudit_PassesPaginationParams_ToService()
    {
        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 2, 5, 0, 0));

        await _factory.CreateClientWithRole(Roles.Admin)
            .GetAsync("/audit?Page=2&PageSize=5");

        _factory.AuditService.Verify(
            s => s.GetPagedAsync(
                It.Is<AuditFilter>(f => f.Page == 2 && f.PageSize == 5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Role hierarchy ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAudit_AdminRoleGrantsAccess_ViaHierarchyTransformation()
    {
        // Admin should satisfy AdminOnly policy (trivially), but this test explicitly
        // confirms the hierarchy transformation doesn't break the Admin claim.
        _factory.AuditService
            .Setup(s => s.GetPagedAsync(It.IsAny<AuditFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AuditLog>([], 1, 20, 0, 0));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-User",  "admin-user");

        var response = await client.GetAsync("/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
