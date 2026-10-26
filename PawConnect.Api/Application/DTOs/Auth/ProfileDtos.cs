using System.ComponentModel.DataAnnotations;

namespace PawConnect.Api.Application.DTOs.Auth;

public class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string NewPassword { get; set; } = string.Empty;
}