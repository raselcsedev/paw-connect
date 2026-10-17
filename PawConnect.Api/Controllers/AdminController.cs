using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Admin;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController : ControllerBase
{
    private readonly IAccountAdminService _accounts;

    public AdminController(IAccountAdminService accounts)
    {
        _accounts = accounts;
    }

    [HttpPatch("users/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, UpdateAccountStatusRequest request, CancellationToken cancellationToken)
    {
        await _accounts.SetStatusAsync(User.GetUserId(), id, request.Status!.Value, cancellationToken);
        return NoContent();
    }
}