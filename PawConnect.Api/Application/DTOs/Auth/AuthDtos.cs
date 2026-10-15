using System.ComponentModel.DataAnnotations;

namespace PawConnect.Api.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, RegularExpression("^(Adopter|Rescuer)$", ErrorMessage = "Role must be Adopter or Rescuer.")]
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public record UserDto(Guid Id, string Email, string DisplayName, string Role);

public class RefreshTokenRequest
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string RefreshToken { get; set; } = string.Empty;
}

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserDto User);