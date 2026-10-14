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

    public ScratchController(IPetCatalog catalog)
    {
        _catalog = catalog;
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
    public IActionResult GetPets()
    {
        return Ok(_catalog.GetAvailablePets());
    }

    [HttpGet("pets/{id:int}")]
    public IActionResult GetPet(int id)
    {
        var pet = _catalog.GetById(id);

        if (pet is null)
        {
            return NotFound();
        }

        return Ok(pet);
    }
}