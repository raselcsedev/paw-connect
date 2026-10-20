using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Mapping;

public static class PetMappings
{
    public static PetDto ToDto(this Pet pet) => new(
    pet.Id, pet.Name, pet.Species, pet.Breed, pet.AgeInMonths, pet.Size,
    pet.Sex, pet.Description, pet.HealthStatus, pet.IsVaccinated, pet.IsSpayedNeutered,
    pet.SpecialCareRequirements, pet.AvailabilityType, pet.Requirements,
    pet.City, pet.Area, pet.Status, pet.CreatedAtUtc, pet.UpdatedAtUtc,
    pet.IsSuspended, pet.ModerationNote, pet.Owner?.DisplayName);


    public static void ApplyRequest(this Pet pet, CreatePetRequest request)
    {
        pet.Name = request.Name.Trim();
        pet.Species = request.Species!.Value;
        pet.Breed = Clean(request.Breed);
        pet.AgeInMonths = request.AgeInMonths;
        pet.Size = request.Size!.Value;
        pet.Sex = request.Sex;
        pet.Description = Clean(request.Description);
        pet.HealthStatus = Clean(request.HealthStatus);
        pet.IsVaccinated = request.IsVaccinated;
        pet.IsSpayedNeutered = request.IsSpayedNeutered;
        pet.SpecialCareRequirements = Clean(request.SpecialCareRequirements);
        pet.AvailabilityType = request.AvailabilityType!.Value;
        pet.Requirements = Clean(request.Requirements);
        pet.City = request.City.Trim();
        pet.Area = Clean(request.Area);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}