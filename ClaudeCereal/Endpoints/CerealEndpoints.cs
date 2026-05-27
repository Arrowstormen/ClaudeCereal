using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCereal.Endpoints;

public static class CerealEndpoints
{
    public static void MapCerealEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cereals");

        group.MapGet("/", async (ICerealService service) =>
            Results.Ok(await service.GetAllAsync()));

        group.MapGet("/{id:int}", async (int id, ICerealService service) =>
            await service.GetByIdAsync(id) is Cereal cereal
                ? Results.Ok(cereal)
                : Results.NotFound());

        group.MapPost("/", async (CerealRequest request, ICerealService service) =>
        {
            var created = await service.CreateAsync(request);
            return Results.Created($"/cereals/{created.Id}", created);
        }).RequireAuthorization();

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
        });

        group.MapDelete("/{id:int}", async (int id, ICerealService service) =>
            await service.DeleteAsync(id)
                ? Results.NoContent()
                : Results.NotFound()).RequireAuthorization();

        group.MapGet("/{id:int}/image", async (int id, ICerealService service, ICerealImageService imageService) =>
        {
            var cereal = await service.GetByIdAsync(id);
            if (cereal is null) return Results.NotFound();

            var imagePath = imageService.GetImagePath(cereal.Name);
            if (imagePath is null) return Results.NotFound();

            new FileExtensionContentTypeProvider().TryGetContentType(imagePath, out var contentType);
            return Results.File(imagePath, contentType ?? "application/octet-stream");
        });
    }
}
