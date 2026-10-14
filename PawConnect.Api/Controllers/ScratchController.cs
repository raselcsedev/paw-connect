using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/scratch")]
public class ScratchController : ControllerBase
{
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
}