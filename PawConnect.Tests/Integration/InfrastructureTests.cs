using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Infrastructure.Data;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class InfrastructureTests(ApiFactory factory)
{
    [Fact]
    public void TheAppRunsInTheTestingEnvironment()
    {
        var environment = factory.Services.GetRequiredService<IHostEnvironment>();

        Assert.Equal("Testing", environment.EnvironmentName);
    }

    [Fact]
    public async Task TheAppUsesTheMigratedTestDatabase_NotTheDevelopmentOne()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var database = new MySqlConnectionStringBuilder(db.Database.GetConnectionString()).Database;

        Assert.EndsWith("_test", database);
        Assert.True(await db.Database.CanConnectAsync(Ct));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact]
    public async Task StartupSeeding_CreatedTheRolesAndTheTestAdmin()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = await db.Roles.Select(role => role.Name).ToListAsync(Ct);

        Assert.Contains("Adopter", roles);
        Assert.Contains("Rescuer", roles);
        Assert.Contains("Admin", roles);

        var admin = await users.FindByEmailAsync(TestSettings.AdminEmail);

        Assert.NotNull(admin);
        Assert.True(await users.IsInRoleAsync(admin, "Admin"));
    }

    [Fact]
    public async Task DevelopmentDemoData_IsNeverSeededOutsideDevelopment()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await users.FindByEmailAsync("demo.rescuer@example.com"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/swagger")]
    [InlineData("/openapi/v1.json")]
    public async Task ApiDocumentation_IsNotExposedOutsideDevelopment(string url)
    {
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_IsPublic_AndDoesNotRevealDataCounts()
    {
        using var client = factory.CreateApiClient();

        var health = await client.GetAsync("/api/health", Ct);
        await health.ShouldBe(HttpStatusCode.OK);

        var database = await client.GetAsync("/api/health/db", Ct);
        await database.ShouldBe(HttpStatusCode.OK);

        var body = await database.Content.ReadAsStringAsync(Ct);
        Assert.Contains("connected", body);
        Assert.DoesNotContain("petCount", body, StringComparison.OrdinalIgnoreCase);
    }
}