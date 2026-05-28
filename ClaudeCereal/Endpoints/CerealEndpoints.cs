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
    }
}
