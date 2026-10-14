using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.Services;

public class InMemoryPetCatalog : IPetCatalog
{
    private readonly List<Pet> _pets = new()
    {
        new Pet { Id = 1, Name = "Milo", Species = Species.Dog, Breed = "Labrador Mix",
                  AgeInMonths = 18, Size = PetSize.Medium, Status = ListingStatus.Available },
        new Pet { Id = 2, Name = "Luna", Species = Species.Cat, Breed = "Tabby",
                  AgeInMonths = 8, Size = PetSize.Small, Status = ListingStatus.Available },
        new Pet { Id = 3, Name = "Rocky", Species = Species.Dog, Breed = "Beagle",
                  AgeInMonths = 36, Size = PetSize.Medium, Status = ListingStatus.Adopted }
    };

    public IReadOnlyList<Pet> GetAvailablePets()
    {
        return _pets.Where(p => p.IsBrowsable()).ToList();
    }

    public Pet? GetById(int id)
    {
        return _pets.FirstOrDefault(p => p.Id == id);
    }
}