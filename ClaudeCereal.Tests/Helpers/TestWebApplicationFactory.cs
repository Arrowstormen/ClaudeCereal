using System.Net.Http;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ClaudeCereal.Tests.Helpers;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> configured for integration testing:
/// <list type="bullet">
///   <item>Sets the environment to "Test" so the CSV seeder is skipped.</item>
///   <item>Replaces <see cref="ICerealService"/> and <see cref="IAuditService"/> with Moq mocks.</item>
///   <item>Replaces Basic auth with <see cref="TestAuthHandler"/> so tests control roles via headers.</item>
/// </list>
/// Use <see cref="CreateClientWithRole"/> or <see cref="CreateUnauthenticatedClient"/> to obtain
/// pre-configured <see cref="HttpClient"/> instances.
/// Call <see cref="ResetMocks"/> (typically in each test constructor) to avoid mock bleed-through.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<ICerealService> CerealService { get; } = new();
    public Mock<IAuditService>  AuditService  { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICerealService>();
            services.RemoveAll<IAuditService>();
            services.AddSingleton(CerealService.Object);
            services.AddSingleton(AuditService.Object);

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, null);

            // PostConfigure runs last and overrides whatever the app set as the default scheme
            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                opts.DefaultForbidScheme       = TestAuthHandler.SchemeName;
            });
        });
    }

    // Creates a client that authenticates with the given single role.
    public HttpClient CreateClientWithRole(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderRoles, role);
        return client;
    }

    // Creates a client that presents no authentication credentials.
    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderUnauthenticated, "true");
        return client;
    }

    // Resets all mock setups and call history — call at the start of each test
    // to prevent setup from one test leaking into the next.
    public void ResetMocks()
    {
        CerealService.Reset();
        AuditService.Reset();
    }
}
