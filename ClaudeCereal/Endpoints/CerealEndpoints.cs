using ClaudeCereal.Models;
using ClaudeCereal.Services;

namespace ClaudeCereal.Endpoints;

public static class CerealEndpoints
{
    public static void MapCerealEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cereals");

        group.MapGet("/", async ([AsParameters] CerealFilter filter, ICerealService service) =>
            Results.Ok(await service.GetFilteredAsync(filter)));

        group.MapGet("/{id:int}", async (int id, ICerealService service) =>
            await service.GetByIdAsync(id) is Cereal cereal
                ? Results.Ok(cereal)
                : Results.NotFound());

        group.MapPost("/", async (Cereal cereal, ICerealService service) =>
        {
            var created = await service.CreateAsync(cereal);
            return Results.Created($"/cereals/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, Cereal input, ICerealService service) =>
            await service.UpdateAsync(id, input) is Cereal updated
                ? Results.Ok(updated)
                : Results.NotFound());

        group.MapDelete("/{id:int}", async (int id, ICerealService service) =>
            await service.DeleteAsync(id)
                ? Results.NoContent()
                : Results.NotFound());
    }
}
