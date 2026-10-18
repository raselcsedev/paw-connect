using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Dashboard;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Counts and recent activity for the signed-in applicant.</summary>
    [HttpGet("applicant")]
    [Authorize(Policy = Policies.ApplyForPets)]
    [ProducesResponseType(typeof(ApplicantDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApplicantDashboardDto>> Applicant(CancellationToken cancellationToken)
    {
        NoStore();
        return Ok(await _dashboard.GetApplicantAsync(User.GetUserId(), cancellationToken));
    }

    /// <summary>Listing and application statistics for the signed-in rescuer.</summary>
    [HttpGet("rescuer")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(RescuerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RescuerDashboardDto>> Rescuer(CancellationToken cancellationToken)
    {
        NoStore();
        return Ok(await _dashboard.GetRescuerAsync(User.GetUserId(), cancellationToken));
    }

    /// <summary>Platform-wide statistics. Administrators only.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardDto>> Admin(CancellationToken cancellationToken)
    {
        NoStore();
        return Ok(await _dashboard.GetAdminAsync(cancellationToken));
    }

    private void NoStore() => Response.Headers.CacheControl = "private, no-store";
}