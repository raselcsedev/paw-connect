using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public enum PetSort
{
    Newest,
    Oldest,
    AgeAsc,
    AgeDesc,
    NameAsc
}

public class PetQueryParameters : IValidatableObject
{
    [StringLength(100)]
    public string? Q { get; set; }

    [EnumDataType(typeof(Species))]
    public Species? Species { get; set; }

    [EnumDataType(typeof(PetSize))]
    public PetSize? Size { get; set; }

    [Range(0, 360)]
    public int? MinAgeMonths { get; set; }

    [Range(0, 360)]
    public int? MaxAgeMonths { get; set; }

    [EnumDataType(typeof(AvailabilityType))]
    public AvailabilityType? AvailabilityType { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    public bool? Vaccinated { get; set; }

    [EnumDataType(typeof(PetSort))]
    public PetSort Sort { get; set; } = PetSort.Newest;

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinAgeMonths.HasValue && MaxAgeMonths.HasValue && MinAgeMonths > MaxAgeMonths)
        {
            yield return new ValidationResult(
                "MinAgeMonths cannot be greater than MaxAgeMonths.",
                new[] { nameof(MinAgeMonths), nameof(MaxAgeMonths) });
        }
    }
}