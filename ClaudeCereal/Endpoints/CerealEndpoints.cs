using ClaudeCereal.Models;
using ClaudeCereal.Services;

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

        group.MapPost("/", async (CerealDto dto, ICerealService service) =>
        {
            var created = await service.CreateAsync(dto);
            return Results.Created($"/cereals/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, CerealDto dto, ICerealService service) =>
            await service.UpdateAsync(id, dto) is Cereal updated
                ? Results.Ok(updated)
                : Results.NotFound());

        group.MapDelete("/{id:int}", async (int id, ICerealService service) =>
            await service.DeleteAsync(id)
                ? Results.NoContent()
                : Results.NotFound());
    }
}
