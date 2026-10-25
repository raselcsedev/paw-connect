using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.Mapping;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Tests;

public class PetMappingsTests
{
    private static CreatePetRequest Request() => new()
    {
        Name = "  Bella  ",
        Species = Species.Rabbit,
        Breed = "   ",
        AgeInMonths = 24,
        Size = PetSize.Small,
        Sex = PetSex.Female,
        Description = " Calm and friendly. ",
        HealthStatus = "",
        IsVaccinated = true,
        IsSpayedNeutered = true,
        SpecialCareRequirements = null,
        AvailabilityType = AvailabilityType.Both,
        Requirements = "\n",
        City = "  Dhaka ",
        Area = " Mirpur ",
    };

    [Fact]
    public void ApplyRequest_TrimsText_AndTurnsBlankOptionalFieldsIntoNull()
    {
        var pet = new Pet();

        pet.ApplyRequest(Request());

        Assert.Equal("Bella", pet.Name);
        Assert.Null(pet.Breed);
        Assert.Equal("Calm and friendly.", pet.Description);
        Assert.Null(pet.HealthStatus);
        Assert.Null(pet.SpecialCareRequirements);
        Assert.Null(pet.Requirements);
        Assert.Equal("Dhaka", pet.City);
        Assert.Equal("Mirpur", pet.Area);
    }

    [Fact]
    public void ApplyRequest_CopiesTheEditableFields()
    {
        var pet = new Pet();

        pet.ApplyRequest(Request());

        Assert.Equal(Species.Rabbit, pet.Species);
        Assert.Equal(24, pet.AgeInMonths);
        Assert.Equal(PetSize.Small, pet.Size);
        Assert.Equal(PetSex.Female, pet.Sex);
        Assert.True(pet.IsVaccinated);
        Assert.True(pet.IsSpayedNeutered);
        Assert.Equal(AvailabilityType.Both, pet.AvailabilityType);
    }

    [Fact]
    public void ApplyRequest_NeverTouchesIdentityOwnerOrStatus_SoOverPostingCannotWork()
    {
        var owner = Guid.NewGuid();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var pet = new Pet { Id = 7, OwnerId = owner, Status = ListingStatus.Available, CreatedAtUtc = created };

        pet.ApplyRequest(Request());

        Assert.Equal(7, pet.Id);
        Assert.Equal(owner, pet.OwnerId);
        Assert.Equal(ListingStatus.Available, pet.Status);
        Assert.Equal(created, pet.CreatedAtUtc);
    }
}