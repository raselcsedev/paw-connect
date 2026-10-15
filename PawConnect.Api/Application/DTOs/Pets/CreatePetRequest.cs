using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public class CreatePetRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EnumDataType(typeof(Species))]
    public Species? Species { get; set; }

    [StringLength(100)]
    public string? Breed { get; set; }

    [Range(0, 360)]
    public int AgeInMonths { get; set; }

    [Required, EnumDataType(typeof(PetSize))]
    public PetSize? Size { get; set; }
}