using ClaudeCereal.Authentication;
using ClaudeCereal.Data;
using ClaudeCereal.Endpoints;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=cereals.db"));

builder.Services.AddScoped<ICerealService, CerealService>();

var imagePath = Path.Combine(
    AppContext.BaseDirectory,
    builder.Configuration["ImagePath"] ?? "Cereal Pictures");
builder.Services.AddSingleton<ICerealImageService>(new CerealImageService(imagePath));

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);
builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var csvPath = builder.Configuration["CsvPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "Data", "cereal.csv");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CerealSeeder.SeedAsync(db, csvPath);
}

app.MapCerealEndpoints();

app.Run();
