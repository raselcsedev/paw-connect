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
    private readonly IApplicationReviewService _review;

    public ApplicationsController(IApplicationService applications, IApplicationReviewService review)
    {
        _applications = applications;
        _review = review;
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

    /// <summary>Applications for the signed-in rescuer's pets, filterable by status, type and pet.</summary>
    [HttpGet("incoming")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(PagedResult<IncomingApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<IncomingApplicationDto>>> GetIncoming(
        [FromQuery] IncomingApplicationsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _review.GetIncomingAsync(User.ToCurrentUser(), query, cancellationToken));
    }

    /// <summary>Applications for one of the signed-in rescuer's pets.</summary>
    [HttpGet("/api/pets/{petId:int}/applications")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(PagedResult<IncomingApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<IncomingApplicationDto>>> GetForPet(
        int petId, [FromQuery] IncomingApplicationsQuery query, CancellationToken cancellationToken)
    {
        query.PetId = petId;
        return Ok(await _review.GetIncomingAsync(User.ToCurrentUser(), query, cancellationToken));
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

    /// <summary>Review, approve or reject an application. Send the version you last saw.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationDto>> UpdateStatus(
        int id, UpdateApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _review.ChangeStatusAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    /// <summary>Withdraw your own pending application.</summary>
    [HttpPost("{id:int}/withdraw")]
    [Authorize(Policy = Policies.ApplyForPets)]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApplicationDto>> Withdraw(int id, CancellationToken cancellationToken)
    {
        return Ok(await _review.WithdrawAsync(id, User.ToCurrentUser(), cancellationToken));
    }
}