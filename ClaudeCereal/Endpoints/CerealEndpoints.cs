using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClaudeCereal.Endpoints;

public static class CerealEndpoints
{
    public static void MapCerealEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cereals");

        group.MapGet("/", async (ICerealService service, int page = 1, int pageSize = 50) =>
            Results.Ok(await service.GetAllAsync(page, pageSize)))
            .Produces<IEnumerable<Cereal>>();

        group.MapGet("/{id:int}", async (int id, ICerealService service) =>
            await service.GetByIdAsync(id) is Cereal cereal
                ? Results.Ok(cereal)
                : Results.NotFound())
            .Produces<Cereal>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", async (Cereal cereal, ICerealService service) =>
        {
            var created = await service.CreateAsync(cereal);
            return Results.Created($"/cereals/{created.Id}", created);
        })
            .Produces<Cereal>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", async (int id, Cereal input, ICerealService service) =>
            await service.UpdateAsync(id, input) is Cereal updated
                ? Results.Ok(updated)
                : Results.NotFound())
            .Produces<Cereal>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", async (int id, ICerealService service) =>
            await service.DeleteAsync(id)
                ? Results.NoContent()
                : Results.NotFound())
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
