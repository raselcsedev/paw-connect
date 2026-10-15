using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Interfaces;

public record RefreshTokenResult(string Token, string Hash, DateTime ExpiresAtUtc);

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(ApplicationUser user, string role);
    RefreshTokenResult CreateRefreshToken();
    string HashToken(string token);
}