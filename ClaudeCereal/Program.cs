using ClaudeCereal.Data;
using ClaudeCereal.Endpoints;
using ClaudeCereal.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=cereals.db"));

builder.Services.AddScoped<ICerealService, CerealService>();
builder.Services.AddScoped<CerealSeeder>();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var csvPath = builder.Configuration["CsvPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "cereal.csv");

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var seeder = scope.ServiceProvider.GetRequiredService<CerealSeeder>();
await seeder.SeedAsync(db, csvPath);

app.MapCerealEndpoints();

app.Run();
