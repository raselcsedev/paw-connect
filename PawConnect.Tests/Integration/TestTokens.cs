using System.Buffers.Text;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PawConnect.Tests.Integration;

/// <summary>Builds tokens a real client could never obtain, to prove the API rejects them.</summary>
public static class TestTokens
{
    public static string Signed(Guid userId, string role, DateTime notBefore, DateTime expires, string? key = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("email", "forged@example.com"),
                new Claim("name", "Forged"),
                new Claim("role", role),
            ]),
            Issuer = TestSettings.Issuer,
            Audience = TestSettings.Audience,
            IssuedAt = notBefore,
            NotBefore = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? TestSettings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public static string Valid(Guid userId, string role) =>
        Signed(userId, role, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

    /// <summary>The classic "alg: none" attack: a token that claims it needs no signature.</summary>
    public static string AlgNone(Guid userId, string role)
    {
        static string Encode(string json) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));

        var expires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var header = Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Encode(
            $"{{\"sub\":\"{userId}\",\"role\":\"{role}\",\"iss\":\"{TestSettings.Issuer}\"," +
            $"\"aud\":\"{TestSettings.Audience}\",\"exp\":{expires}}}");

        return $"{header}.{payload}.";
    }
}