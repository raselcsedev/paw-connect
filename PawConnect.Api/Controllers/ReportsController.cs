using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/pets/{petId:int}/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    /// <summary>Report a listing. One open report per user per listing.</summary>
    [HttpPost]
    [EnableRateLimiting(Policies.ReportRateLimit)]
    [ProducesResponseType(typeof(ReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ReportDto>> Create(
        int petId, CreateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await _reports.SubmitAsync(petId, request, User.ToCurrentUser(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, report);
    }
}