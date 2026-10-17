using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applications;

    public ApplicationsController(IApplicationService applications)
    {
        _applications = applications;
    }

    /// <summary>Submit an adoption or foster application for a pet.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ApplyForPets)]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationDto>> Submit(
        CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        var application = await _applications.SubmitAsync(request, User.ToCurrentUser(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, application);
    }

    /// <summary>The signed-in applicant's own applications.</summary>
    [HttpGet("my")]
    [Authorize(Policy = Policies.ApplyForPets)]
    [ProducesResponseType(typeof(PagedResult<ApplicationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ApplicationSummaryDto>>> GetMine(
        [FromQuery] MyApplicationsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _applications.GetMineAsync(User.ToCurrentUser(), query, cancellationToken));
    }

    /// <summary>Visible only to the applicant and to the owner of the pet.</summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _applications.GetByIdAsync(id, User.ToCurrentUser(), cancellationToken));
    }
}