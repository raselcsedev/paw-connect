using PawConnect.Api.Application.DTOs.Auth;

namespace PawConnect.Tests.Integration;

public sealed record TestUser(
    Guid Id,
    string Email,
    string Password,
    string DisplayName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime IssuedAtUtc)
{
    public static TestUser From(AuthResponse auth, string email, string password) => new(
        auth.User.Id, email, password, auth.User.DisplayName, auth.User.Role,
        auth.AccessToken, auth.RefreshToken, DateTime.UtcNow);
}