using ClaudeCereal.Data;
using ClaudeCereal.Models;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=cereals.db"));

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Seed database from CSV on startup
var csvPath = builder.Configuration["CsvPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "cereal.csv");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CerealSeeder.SeedAsync(db, csvPath);
}

// GET /cereals
app.MapGet("/cereals", async (AppDbContext db) =>
    await db.Cereals.ToListAsync());

// GET /cereals/{id}
app.MapGet("/cereals/{id:int}", async (int id, AppDbContext db) =>
    await db.Cereals.FindAsync(id) is Cereal cereal
        ? Results.Ok(cereal)
        : Results.NotFound());

// POST /cereals
app.MapPost("/cereals", async (Cereal cereal, AppDbContext db) =>
{
    db.Cereals.Add(cereal);
    await db.SaveChangesAsync();
    return Results.Created($"/cereals/{cereal.Id}", cereal);
});

// PUT /cereals/{id}
app.MapPut("/cereals/{id:int}", async (int id, Cereal input, AppDbContext db) =>
{
    var cereal = await db.Cereals.FindAsync(id);
    if (cereal is null) return Results.NotFound();

    cereal.Name     = input.Name;
    cereal.Mfr      = input.Mfr;
    cereal.Type     = input.Type;
    cereal.Calories = input.Calories;
    cereal.Protein  = input.Protein;
    cereal.Fat      = input.Fat;
    cereal.Sodium   = input.Sodium;
    cereal.Fiber    = input.Fiber;
    cereal.Carbo    = input.Carbo;
    cereal.Sugars   = input.Sugars;
    cereal.Potass   = input.Potass;
    cereal.Vitamins = input.Vitamins;
    cereal.Shelf    = input.Shelf;
    cereal.Weight   = input.Weight;
    cereal.Cups     = input.Cups;
    cereal.Rating   = input.Rating;

    await db.SaveChangesAsync();
    return Results.Ok(cereal);
});

// DELETE /cereals/{id}
app.MapDelete("/cereals/{id:int}", async (int id, AppDbContext db) =>
{
    var cereal = await db.Cereals.FindAsync(id);
    if (cereal is null) return Results.NotFound();

    db.Cereals.Remove(cereal);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
