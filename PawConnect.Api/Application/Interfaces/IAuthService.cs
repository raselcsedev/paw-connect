using PawConnect.Api.Application.DTOs.Auth;

namespace PawConnect.Api.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
}