using PawConnect.Tests.Integration;

namespace PawConnect.Tests;

public class TestSettingsTests
{
    [Theory]
    [InlineData("Server=localhost;Database=paw_connect_test;User=u;Password=p;")]
    [InlineData("Server=localhost;Database=PAW_CONNECT_TEST;User=u;Password=p;")]
    public void ADatabaseEndingInTest_IsAccepted(string connection)
    {
        TestSettings.EnsureItIsATestDatabase(connection);
    }

    [Theory]
    [InlineData("Server=localhost;Database=paw_connect;User=u;Password=p;")]
    [InlineData("Server=localhost;Database=test_paw_connect;User=u;Password=p;")]
    [InlineData("Server=localhost;Database=mysql;User=u;Password=p;")]
    [InlineData("Server=localhost;User=u;Password=p;")]
    public void AnythingElse_IsRefused_SoTheDevelopmentDatabaseIsNeverWiped(string connection)
    {
        Assert.Throws<InvalidOperationException>(() => TestSettings.EnsureItIsATestDatabase(connection));
    }
}