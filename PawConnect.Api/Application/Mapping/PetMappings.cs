using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Mapping;

public static class PetMappings
{
    public static PetDto ToDto(this Pet pet) => new(
        pet.Id,
        pet.Name,
        pet.Species,
        pet.Breed,
        pet.AgeInMonths,
        pet.Size,
        pet.Status,
        pet.CreatedAtUtc);
}