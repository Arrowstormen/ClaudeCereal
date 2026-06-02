using System.Text.Json.Serialization;
using ClaudeCereal.Authentication;
using ClaudeCereal.Data;
using ClaudeCereal.Endpoints;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// AuditInterceptor is scoped so it can access IHttpContextAccessor (per-request state)
// and safely hold mutable fields (_pendingEntries, _transaction) without thread-safety concerns.
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=cereals.db");
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});

builder.Services.AddScoped<ICerealService, CerealService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var imagePath = Path.Combine(
    AppContext.BaseDirectory,
    builder.Configuration["ImagePath"] ?? "Cereal Pictures");
builder.Services.AddSingleton<ICerealImageService>(new CerealImageService(imagePath));

builder.Services.Configure<BasicAuthSettings>(builder.Configuration.GetSection("BasicAuth"));

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddScoped<IClaimsTransformation, RoleHierarchyTransformation>();

builder.Services.AddAuthorization(options =>
{
    // Any authenticated user has at least Reader (via the hierarchy transformation)
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    options.AddPolicy(Policies.ReaderOrAbove, p => p.RequireAuthenticatedUser());
    // Editor and Admin both carry the Editor claim after transformation
    options.AddPolicy(Policies.EditorOrAbove, p => p.RequireRole(Roles.Editor));
    options.AddPolicy(Policies.AdminOnly,     p => p.RequireRole(Roles.Admin));
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Basic"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Enter a username and password (reader / editor / admin)"
            }
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // AllowAnonymous so the FallbackPolicy doesn't force browser-level Basic Auth
    // on the spec and UI endpoints — credentials are entered inside Scalar instead.
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("Basic"))
        .AllowAnonymous();
}

var csvPath = builder.Configuration["CsvPath"]
    ?? Path.Combine(AppContext.BaseDirectory, "Data", "cereal.csv");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CerealSeeder.SeedAsync(db, csvPath);
}

app.MapCerealEndpoints();
app.MapAuditEndpoints();

app.Run();
