using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/scratch")]
public class ScratchController : ControllerBase
{
    private readonly IPetCatalog _catalog;
    private readonly ILogger<ScratchController> _logger;

    public ScratchController(IPetCatalog catalog, ILogger<ScratchController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [HttpGet("pet")]
    public IActionResult GetSamplePet()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Milo",
            Species = Species.Dog,
            Breed = "Labrador Mix",
            AgeInMonths = 18,
            Size = PetSize.Medium,
            Status = ListingStatus.Available
        };

        return Ok(new { pet, isBrowsable = pet.IsBrowsable() });
    }

    [HttpGet("pets")]
    public async Task<IActionResult> GetPets(CancellationToken cancellationToken)
    {
        var pets = await _catalog.GetAvailablePetsAsync(cancellationToken);
        return Ok(pets);
    }

    [HttpGet("pets/{id:int}")]
    public async Task<IActionResult> GetPet(int id, CancellationToken cancellationToken)
    {
        var pet = await _catalog.GetByIdAsync(id, cancellationToken);

        if (pet is null)
        {
            return NotFound();
        }

        return Ok(pet);
    }

    [HttpGet("slow")]
    public async Task<IActionResult> GetSlow(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Slow request started");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return Ok(new { message = "Finished after 5 seconds" });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Slow request was cancelled by the client");
            throw;
        }
    }
}