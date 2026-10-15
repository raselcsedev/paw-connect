using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(ApplicationUser user, string role);
}