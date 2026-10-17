using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/pets")]
public class PetsController : ControllerBase
{
    private readonly IPetService _pets;

    public PetsController(IPetService pets)
    {
        _pets = pets;
    }

    /// <summary>Browse available pets with filtering, sorting and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PetSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PetSummaryDto>>> Search(
        [FromQuery] PetQueryParameters query, CancellationToken cancellationToken)
    {
        return Ok(await _pets.SearchAsync(query, cancellationToken));
    }

    /// <summary>The signed-in rescuer's own listings, including drafts and archived ones.</summary>
    [HttpGet("mine")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(PagedResult<PetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PetDto>>> GetMine(
        [FromQuery] MyPetsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _pets.GetMineAsync(User.ToCurrentUser(), query, cancellationToken));
    }

    /// <summary>Drafts and archived listings are visible only to their owner and administrators.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _pets.GetByIdAsync(id, User.ToCurrentUserOrNull(), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PetDto>> Create(CreatePetRequest request, CancellationToken cancellationToken)
    {
        var pet = await _pets.CreateAsync(request, User.ToCurrentUser(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.ManagePets)]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PetDto>> Update(
        int id, UpdatePetRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _pets.UpdateAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    [HttpPatch("{id:int}/availability")]
    [Authorize(Policy = Policies.ModeratePets)]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PetDto>> ChangeAvailability(
        int id, ChangeAvailabilityRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _pets.ChangeAvailabilityAsync(id, request, User.ToCurrentUser(), cancellationToken));
    }

    /// <summary>Archives the listing (soft delete). Archiving twice is allowed.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.ModeratePets)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        await _pets.ArchiveAsync(id, User.ToCurrentUser(), cancellationToken);
        return NoContent();
    }
}