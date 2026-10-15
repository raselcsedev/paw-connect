using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.Interfaces;

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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PetSummaryDto>>> GetAvailable(CancellationToken cancellationToken)
    {
        return Ok(await _pets.GetAvailableAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _pets.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PetDto>> Create(CreatePetRequest request, CancellationToken cancellationToken)
    {
        var pet = await _pets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = pet.Id }, pet);
    }

    [HttpPatch("{id:int}/availability")]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PetDto>> ChangeAvailability(
        int id, ChangeAvailabilityRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _pets.ChangeAvailabilityAsync(id, request, cancellationToken));
    }
}