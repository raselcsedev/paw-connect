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

    [EnumDataType(typeof(PetSex))]
    public PetSex Sex { get; set; } = PetSex.Unknown;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? HealthStatus { get; set; }

    public bool IsVaccinated { get; set; }
    public bool IsSpayedNeutered { get; set; }

    [StringLength(1000)]
    public string? SpecialCareRequirements { get; set; }

    [Required, EnumDataType(typeof(AvailabilityType))]
    public AvailabilityType? AvailabilityType { get; set; }

    [StringLength(1000)]
    public string? Requirements { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Area { get; set; }
}

public class UpdatePetRequest : CreatePetRequest
{
}