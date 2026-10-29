using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawConnect.Api;
using PawConnect.Api.Infrastructure.Data;

[assembly: AssemblyFixture(typeof(PawConnect.Tests.Integration.ApiFactory))]

namespace PawConnect.Tests.Integration;

/// <summary>
/// Starts the real API once for the whole test run, against a freshly migrated test database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<ApiAssemblyMarker>, IAsyncLifetime
{
    private readonly string _storageRoot =
        Path.Combine(Path.GetTempPath(), "pawconnect-integration", Guid.NewGuid().ToString("N"));

    private readonly SemaphoreSlim _sharedUsersGate = new(1, 1);
    private readonly Dictionary<string, TestUser> _sharedUsers = new();

    public ApiFactory()
    {
        // Settings that Program.cs reads while it is still starting up must come from environment variables,
        // which exist before the app builds its configuration.
        var settings = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__DefaultConnection"] = TestSettings.ConnectionString,
            ["Jwt__SigningKey"] = TestSettings.SigningKey,
            ["Jwt__Issuer"] = TestSettings.Issuer,
            ["Jwt__Audience"] = TestSettings.Audience,
            ["Seed__AdminEmail"] = TestSettings.AdminEmail,
            ["Seed__AdminPassword"] = TestSettings.AdminPassword,
            ["Storage__RootPath"] = _storageRoot,

            // Every test client looks like the same address, so raise the limits. RateLimitTests lowers them on purpose.
            ["RateLimiting__Auth__PermitLimit"] = "100000",
            ["RateLimiting__Submit__PermitLimit"] = "100000",
            ["RateLimiting__Report__PermitLimit"] = "100000",
        };

        foreach (var (key, value) in settings)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();

            // Set PAWCONNECT_TEST_LOGS=1 to see the server's logs while debugging a failing test.
            if (Environment.GetEnvironmentVariable("PAWCONNECT_TEST_LOGS") == "1")
            {
                logging.AddConsole();
            }
        });
    }

    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_storageRoot);

        // A clean database for every run: delete it, then apply all migrations.
        await using (var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(TestSettings.ConnectionString, new MariaDbServerVersion(TestSettings.MariaDbVersion))
                .Options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        // Start the app now, so any startup problem shows up here and not in a random test.
        using var warmup = CreateApiClient();

        var environment = Services.GetRequiredService<IHostEnvironment>();
        if (environment.EnvironmentName != "Testing")
        {
            throw new InvalidOperationException(
                $"The app under test started in '{environment.EnvironmentName}', not 'Testing'. Refusing to continue.");
        }
    }

    /// <summary>
    /// A user shared by read-only tests (permission checks). Never change or disable a shared user: register your own.
    /// Tokens last 15 minutes, so a shared user is logged in again when its token gets old.
    /// </summary>
    public async Task<TestUser> SharedUserAsync(string role)
    {
        await _sharedUsersGate.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            _sharedUsers.TryGetValue(role, out var existing);

            if (existing is not null && DateTime.UtcNow - existing.IssuedAtUtc < TimeSpan.FromMinutes(10))
            {
                return existing;
            }

            var user = role == "Admin"
                ? await this.LoginAdminAsync()
                : existing is null
                    ? await this.RegisterAsync(role)
                    : await this.LoginAsync(existing.Email, existing.Password);

            _sharedUsers[role] = user;
            return user;
        }
        finally
        {
            _sharedUsersGate.Release();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        try
        {
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless.
        }
    }
}