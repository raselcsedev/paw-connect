using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminModerationController : ControllerBase
{
    private readonly IModerationService _moderation;

    public AdminModerationController(IModerationService moderation)
    {
        _moderation = moderation;
    }

    /// <summary>All listings, including drafts, suspended and archived ones.</summary>
    [HttpGet("pets")]
    [ProducesResponseType(typeof(PagedResult<AdminPetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminPetDto>>> GetPets(
        [FromQuery] AdminPetsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _moderation.GetPetsAsync(query, cancellationToken));
    }

    /// <summary>Suspend, restore or archive a listing.</summary>
    [HttpPatch("pets/{id:int}/moderation")]
    [ProducesResponseType(typeof(AdminPetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminPetDto>> ModeratePet(
        int id, ModeratePetRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _moderation.ModeratePetAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    /// <summary>The report queue. Open reports come first.</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(PagedResult<AdminReportSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminReportSummaryDto>>> GetReports(
        [FromQuery] AdminReportsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _moderation.GetReportsAsync(query, cancellationToken));
    }

    [HttpGet("reports/{id:int}")]
    [ProducesResponseType(typeof(AdminReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminReportDto>> GetReport(int id, CancellationToken cancellationToken)
    {
        return Ok(await _moderation.GetReportAsync(id, cancellationToken));
    }

    /// <summary>Resolve or dismiss a report, optionally suspending or archiving the listing.</summary>
    [HttpPatch("reports/{id:int}")]
    [ProducesResponseType(typeof(AdminReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminReportDto>> ReviewReport(
        int id, ReviewReportRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _moderation.ReviewReportAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }
}