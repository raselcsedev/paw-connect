using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Tests;

public class TokenServiceTests
{
    private const string Key = "unit-test-signing-key-0123456789-abcdef";
    private const string Issuer = "PawConnect.Tests";
    private const string Audience = "PawConnect.Web";

    private static TokenService CreateService(int accessMinutes = 15, int refreshDays = 14) =>
        new(Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = Key,
            AccessTokenMinutes = accessMinutes,
            RefreshTokenDays = refreshDays,
        }));

    private static TokenValidationParameters Parameters(string key = Key, string audience = Audience) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };

    private static ApplicationUser User() => new()
    {
        Id = Guid.NewGuid(),
        Email = "sam@example.com",
        DisplayName = "Sam Rescuer",
        PasswordHash = "SECRET-HASH-VALUE",
    };

    [Fact]
    public async Task AccessToken_IsValid_AndCarriesTheExpectedClaims()
    {
        var user = User();
        var (token, _) = CreateService().CreateAccessToken(user, "Rescuer");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters());

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.Claims["sub"]);
        Assert.Equal("sam@example.com", result.Claims["email"]);
        Assert.Equal("Sam Rescuer", result.Claims["name"]);
        Assert.Equal("Rescuer", result.Claims["role"]);
        Assert.False(string.IsNullOrWhiteSpace(result.Claims["jti"]?.ToString()));
    }

    [Fact]
    public async Task AccessToken_ContainsNothingBeyondTheDocumentedClaims_AndNeverTheHash()
    {
        var (token, _) = CreateService().CreateAccessToken(User(), "Adopter");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters());

        var allowed = new[] { "sub", "email", "name", "role", "jti", "iss", "aud", "exp", "iat", "nbf" };
        Assert.Empty(result.Claims.Keys.Except(allowed));
        Assert.DoesNotContain(result.Claims.Values, value => value?.ToString()?.Contains("SECRET-HASH") == true);
    }

    [Fact]
    public void AccessToken_ExpiresAfterTheConfiguredLifetime()
    {
        var (_, expiresAt) = CreateService(accessMinutes: 15).CreateAccessToken(User(), "Adopter");

        Assert.InRange(expiresAt, DateTime.UtcNow.AddMinutes(14.9), DateTime.UtcNow.AddMinutes(15.1));
    }

    [Fact]
    public async Task AccessToken_CheckedWithTheWrongKey_IsRejected()
    {
        var (token, _) = CreateService().CreateAccessToken(User(), "Adopter");

        var result = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, Parameters(key: "a-completely-different-signing-key-0123456789"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AccessToken_ForAnotherAudience_IsRejected()
    {
        var (token, _) = CreateService().CreateAccessToken(User(), "Adopter");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters(audience: "someone-else"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AccessToken_WithATamperedPayload_IsRejected()
    {
        var (token, _) = CreateService().CreateAccessToken(User(), "Adopter");
        var parts = token.Split('.');
        parts[1] = (parts[1][0] == 'A' ? 'B' : 'A') + parts[1][1..];

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(string.Join('.', parts), Parameters());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void AccessTokens_EachGetTheirOwnTokenId()
    {
        var service = CreateService();
        var user = User();

        var first = service.CreateAccessToken(user, "Adopter").Token;
        var second = service.CreateAccessToken(user, "Adopter").Token;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RefreshToken_IsLongUrlSafeAndUnique()
    {
        var service = CreateService();

        var tokens = Enumerable.Range(0, 100).Select(_ => service.CreateRefreshToken().Token).ToList();

        Assert.Equal(100, tokens.Distinct().Count());
        Assert.All(tokens, token =>
        {
            Assert.True(token.Length >= 80);
            Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), token);
        });
    }

    [Fact]
    public void RefreshToken_StoresOnlyAHash_ThatIsDeterministicAndDifferentFromTheToken()
    {
        var service = CreateService();
        var refresh = service.CreateRefreshToken();

        Assert.NotEqual(refresh.Token, refresh.Hash);
        Assert.Equal(refresh.Hash, service.HashToken(refresh.Token));
        Assert.Matches(new Regex("^[0-9A-F]{64}$"), refresh.Hash);
        Assert.NotEqual(refresh.Hash, service.HashToken(refresh.Token + "x"));
    }

    [Fact]
    public void RefreshToken_ExpiresAfterTheConfiguredNumberOfDays()
    {
        var refresh = CreateService(refreshDays: 14).CreateRefreshToken();

        Assert.InRange(refresh.ExpiresAtUtc, DateTime.UtcNow.AddDays(13.99), DateTime.UtcNow.AddDays(14.01));
    }
}