using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClaudeCereal.Authentication;
using ClaudeCereal.Exceptions;
using ClaudeCereal.Models;
using ClaudeCereal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ClaudeCereal.Tests.Endpoints;

public class CerealEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CerealEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
    }

    // ── GET /cereals ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCereals_Unauthenticated_Returns401()
    {
        var client   = _factory.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/cereals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCereals_AsReader_Returns200()
    {
        _factory.CerealService
            .Setup(s => s.GetFilteredAsync(It.IsAny<CerealFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Cereal>([], 1, 20, 0, 0));

        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCereals_WithInvalidRange_Returns400()
    {
        // MinCalories > MaxCalories triggers a validation problem (ASP.NET returns 400)
        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals?MinCalories=200&MaxCalories=100");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /cereals/{id} ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetCerealById_Found_Returns200()
    {
        var cereal = new Cereal { Id = 1, Name = "Cheerios" };
        _factory.CerealService
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cereal);

        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCerealById_NotFound_Returns404()
    {
        _factory.CerealService
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cereal?)null);
        _factory.CerealService
            .Setup(s => s.IsDeletedAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCerealById_SoftDeleted_Returns410()
    {
        _factory.CerealService
            .Setup(s => s.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cereal?)null);
        _factory.CerealService
            .Setup(s => s.IsDeletedAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals/42");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    // ── POST /cereals ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCereal_AsReader_Returns403()
    {
        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .PostAsJsonAsync("/cereals", new CerealRequest { Name = "Forbidden" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCereal_AsEditor_Returns201()
    {
        var created = new Cereal { Id = 7, Name = "New Cereal" };
        _factory.CerealService
            .Setup(s => s.CreateAsync(It.IsAny<CerealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PostAsJsonAsync("/cereals", new CerealRequest { Name = "New Cereal" }, TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("/cereals/7", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Fact]
    public async Task CreateCereal_SoftDeletedConflict_Returns409()
    {
        _factory.CerealService
            .Setup(s => s.CreateAsync(It.IsAny<CerealRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SoftDeletedConflictException("Conflict Cereal"));

        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PostAsJsonAsync("/cereals", new CerealRequest { Name = "Conflict Cereal" }, TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── PUT /cereals/{id} ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCereal_AsEditor_Returns200()
    {
        var updated = new Cereal { Id = 5, Name = "Updated" };
        _factory.CerealService
            .Setup(s => s.UpdateAsync(5, It.IsAny<CerealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PutAsJsonAsync("/cereals/5", new CerealRequest { Name = "Updated" }, TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCereal_NotFound_Returns404()
    {
        _factory.CerealService
            .Setup(s => s.UpdateAsync(404, It.IsAny<CerealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cereal?)null);

        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PutAsJsonAsync("/cereals/404", new CerealRequest { Name = "Ghost" }, TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCereal_ConcurrencyConflict_Returns409()
    {
        _factory.CerealService
            .Setup(s => s.UpdateAsync(3, It.IsAny<CerealRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PutAsJsonAsync("/cereals/3", new CerealRequest { Name = "Stale" }, TestJsonOptions.Default);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── DELETE /cereals/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCereal_AsEditor_Returns403()
    {
        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .DeleteAsync("/cereals/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCereal_AsAdmin_Returns204()
    {
        _factory.CerealService
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .DeleteAsync("/cereals/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCereal_NotFound_Returns404()
    {
        _factory.CerealService
            .Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .DeleteAsync("/cereals/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /cereals/{id}/restore ─────────────────────────────────────────────

    [Fact]
    public async Task RestoreCereal_AsEditor_Returns403()
    {
        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PostAsync("/cereals/1/restore", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RestoreCereal_AsAdmin_Returns200()
    {
        var restored = new Cereal { Id = 10, Name = "Restored" };
        _factory.CerealService
            .Setup(s => s.RestoreAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(restored);

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .PostAsync("/cereals/10/restore", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RestoreCereal_AlreadyActive_Returns409()
    {
        _factory.CerealService
            .Setup(s => s.RestoreAsync(11, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CerealAlreadyActiveException(11));

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .PostAsync("/cereals/11/restore", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RestoreCereal_NotFound_Returns404()
    {
        _factory.CerealService
            .Setup(s => s.RestoreAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cereal?)null);

        var response = await _factory.CreateClientWithRole(Roles.Admin)
            .PostAsync("/cereals/999/restore", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── POST /cereals/import ───────────────────────────────────────────────────

    [Fact]
    public async Task ImportCereals_AsReader_Returns403()
    {
        var content = BuildCsvFile("name\r\nTest\r\n");
        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .PostAsync("/cereals/import", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ImportCereals_NoFile_Returns400()
    {
        // Send a multipart body with no actual file part
        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PostAsync("/cereals/import", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ImportCereals_AsEditor_WithCsvFile_Returns200()
    {
        _factory.CerealService
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), ImportFormat.Csv, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImportResult(2, 0, []));

        var content  = BuildCsvFile("name,calories\r\nA,100\r\nB,200\r\n");
        var response = await _factory.CreateClientWithRole(Roles.Editor)
            .PostAsync("/cereals/import", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResult>(TestJsonOptions.Default);
        Assert.NotNull(result);
        Assert.Equal(2, result.Inserted);
    }

    // ── GET /cereals/{id}/image ────────────────────────────────────────────────

    [Fact]
    public async Task GetCerealImage_NotFound_WhenImagePathIsNull()
    {
        var cereal = new Cereal { Id = 20, Name = "No Image Cereal" };
        _factory.CerealService
            .Setup(s => s.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cereal);

        // ICerealImageService is NOT mocked — the real singleton is registered with an empty
        // index (test environment has no image directory).
        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals/20/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCerealImage_Returns404_WhenCerealNotFound()
    {
        _factory.CerealService
            .Setup(s => s.GetByIdAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cereal?)null);

        var response = await _factory.CreateClientWithRole(Roles.Reader)
            .GetAsync("/cereals/21/image");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildCsvFile(string csvContent)
    {
        var bytes      = Encoding.UTF8.GetBytes(csvContent);
        var byteContent = new ByteArrayContent(bytes);
        byteContent.Headers.ContentType =
            new MediaTypeHeaderValue("text/csv");

        var form = new MultipartFormDataContent();
        form.Add(byteContent, "file", "cereals.csv");
        return form;
    }
}
