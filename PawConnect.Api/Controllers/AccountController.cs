using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Change your display name.</summary>
    [HttpPatch("profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _auth.UpdateProfileAsync(User.GetUserId(), request, cancellationToken));
    }

    /// <summary>Change your password. All other sessions are signed out and a new session is returned.</summary>
    [HttpPost("change-password")]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _auth.ChangePasswordAsync(User.GetUserId(), request, cancellationToken));
    }
}