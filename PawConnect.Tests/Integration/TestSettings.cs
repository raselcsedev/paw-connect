using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace PawConnect.Tests.Integration;

public static class TestSettings
{
    public const string SigningKey = "integration-test-signing-key-0123456789-abcdef-xyz";
    public const string Issuer = "PawConnect.Api";
    public const string Audience = "PawConnect.Web";

    public const string AdminEmail = "test.admin@example.com";
    public const string AdminPassword = "Admin#TestPass2026";
    public const string UserPassword = "Test#Passw0rd2026";

    // Must match the server version used in PawConnect.Api/Program.cs.
    public static readonly Version MariaDbVersion = new(10, 4, 32);

    private static readonly Lazy<string> Connection = new(LoadConnectionString);

    public static string ConnectionString => Connection.Value;

    /// <summary>
    /// The test run deletes and recreates this database. Refuse to touch anything that is not clearly a test database.
    /// </summary>
    public static void EnsureItIsATestDatabase(string connectionString)
    {
        var database = new MySqlConnectionStringBuilder(connectionString).Database;

        if (string.IsNullOrWhiteSpace(database) || !database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run: the test database name must end with '_test' but is '{database}'. " +
                "The integration tests delete and recreate this database.");
        }
    }

    private static string LoadConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestSettings).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration["ConnectionStrings:TestDatabase"];

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "No test database is configured. Run:" + Environment.NewLine +
                "dotnet user-secrets set 'ConnectionStrings:TestDatabase' " +
                "'Server=localhost;Port=3306;Database=paw_connect_test;User=...;Password=...;' --project PawConnect.Tests");
        }

        EnsureItIsATestDatabase(connection);
        return connection;
    }
}