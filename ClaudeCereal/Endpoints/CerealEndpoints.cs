using ClaudeCereal.Authentication;
using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Endpoints;

public static class CerealEndpoints
{
    public static void MapCerealEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cereals")
            .RequireAuthorization(Policies.ReaderOrAbove);  // floor for the entire group

        group.MapGet("/", async ([AsParameters] CerealFilter filter, ICerealService service) =>
        {
            var errors = filter.GetValidationErrors();
            if (errors is not null)
                return Results.ValidationProblem(errors);

            return Results.Ok(await service.GetFilteredAsync(filter));
        });

        group.MapGet("/{id:int}", async (int id, ICerealService service) =>
            await service.GetByIdAsync(id) is Cereal cereal
                ? Results.Ok(cereal)
                : Results.NotFound());

        group.MapPost("/", async (CerealRequest request, ICerealService service) =>
        {
            var created = await service.CreateAsync(request);
            return Results.Created($"/cereals/{created.Id}", created);
        }).RequireAuthorization(Policies.EditorOrAbove);

        group.MapPut("/{id:int}", async (int id, CerealRequest request, ICerealService service) =>
        {
            try
            {
                return await service.UpdateAsync(id, request) is Cereal updated
                    ? Results.Ok(updated)
                    : Results.NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict();
            }
        }).RequireAuthorization(Policies.EditorOrAbove);

        group.MapDelete("/{id:int}", async (int id, ICerealService service) =>
            await service.DeleteAsync(id)
                ? Results.NoContent()
                : Results.NotFound())
            .RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/{id:int}/image", async (int id, ICerealService service, ICerealImageService imageService) =>
        {
            var cereal = await service.GetByIdAsync(id);
            if (cereal is null) return Results.NotFound();

            var imagePath = imageService.GetImagePath(cereal.Name);
            if (imagePath is null) return Results.NotFound();

            new FileExtensionContentTypeProvider().TryGetContentType(imagePath, out var contentType);
            return Results.File(imagePath, contentType ?? "application/octet-stream");
        });

        group.MapPost("/import", async (IFormFile file, ICerealService service) =>
        {
            var format = DetectFormat(file.ContentType, file.FileName);
            if (format is null)
                return Results.BadRequest("Unsupported file type. Upload a .csv or .json file.");

            try
            {
                using var stream = file.OpenReadStream();
                var result = await service.ImportAsync(stream, format.Value);
                return Results.Ok(result);
            }
            catch (InvalidDataException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireAuthorization(Policies.EditorOrAbove)
          .DisableAntiforgery();
    }

    private static ImportFormat? DetectFormat(string? contentType, string? fileName)
    {
        if (contentType is not null)
        {
            if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)) return ImportFormat.Json;
            if (contentType.Contains("csv",  StringComparison.OrdinalIgnoreCase)) return ImportFormat.Csv;
            // browsers often send text/plain for .csv files dragged in
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return ImportFormat.Csv;
        }
        return Path.GetExtension(fileName)?.ToLowerInvariant() switch
        {
            ".json" => ImportFormat.Json,
            ".csv"  => ImportFormat.Csv,
            _       => null
        };
    }
}
