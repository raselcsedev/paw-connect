using System.Net;
using Microsoft.Extensions.Configuration;
using PawConnect.Api.Application.DTOs.Auth;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class RateLimitTests(ApiFactory factory)
{
    [Fact]
    public async Task AuthEndpoints_AreLimited_AndTellTheClientWhenToRetry()
    {
        // A second copy of the app with a tiny limit. The shared app keeps its generous one.
        await using var limited = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Auth:PermitLimit"] = "3",
                    ["RateLimiting:Auth:WindowMinutes"] = "1",
                })));

        using var client = limited.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("http://localhost"),
        });

        var body = new LoginRequest { Email = $"nobody.{Guid.NewGuid():N}@example.com", Password = "Wrong#Passw0rd1" };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var allowed = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", body);
            await allowed.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var limitedResponse = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", body);

        await limitedResponse.ShouldBe(HttpStatusCode.TooManyRequests);
        Assert.True(limitedResponse.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter!.Single()) > 0);
        Assert.Equal("application/problem+json", limitedResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TheSharedApp_IsNotAffectedByTheLimitedCopy()
    {
        using var client = factory.CreateApiClient();
        var body = new LoginRequest { Email = $"nobody.{Guid.NewGuid():N}@example.com", Password = "Wrong#Passw0rd1" };

        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", body);
            await response.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}