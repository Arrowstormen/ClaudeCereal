using ClaudeCereal.Data;
using ClaudeCereal.Endpoints;
using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=cereals.db"));

builder.Services.AddScoped<ICerealService, CerealService>();
builder.Services.AddOpenApi();

var odataBuilder = new ODataConventionModelBuilder();
odataBuilder.EntitySet<Cereal>("Cereals");
var edmModel = odataBuilder.GetEdmModel();

builder.Services.AddControllers()
    .AddOData(opt => opt
        .AddRouteComponents("odata", edmModel)
        .Select()
        .Filter()
        .OrderBy()
        .SetMaxTop(100)
        .Count());

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var csvPath = builder.Configuration["CsvPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "cereal.csv");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CerealSeeder.SeedAsync(db, csvPath);
}

app.MapCerealEndpoints();
app.MapControllers();

app.Run();
