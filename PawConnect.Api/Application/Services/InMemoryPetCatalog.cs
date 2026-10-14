using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.Services;

public class InMemoryPetCatalog : IPetCatalog
{
    private readonly List<Pet> _pets = new()
    {
        new Pet { Id = 1, Name = "Milo", Species = Species.Dog, Breed = "Labrador Mix", AgeInMonths = 18,
                  Size = PetSize.Medium, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-10) },
        new Pet { Id = 2, Name = "Luna", Species = Species.Cat, Breed = "Tabby", AgeInMonths = 8,
                  Size = PetSize.Small, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-3) },
        new Pet { Id = 3, Name = "Rocky", Species = Species.Dog, Breed = "Beagle", AgeInMonths = 36,
                  Size = PetSize.Medium, Status = ListingStatus.Adopted, CreatedAtUtc = DateTime.UtcNow.AddDays(-30) },
        new Pet { Id = 4, Name = "Bella", Species = Species.Rabbit, Breed = "Holland Lop", AgeInMonths = 24,
                  Size = PetSize.Small, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-7) },
        new Pet { Id = 5, Name = "Max", Species = Species.Dog, Breed = "German Shepherd", AgeInMonths = 60,
                  Size = PetSize.Large, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-1) },
        new Pet { Id = 6, Name = "Coco", Species = Species.Bird, Breed = "Cockatiel", AgeInMonths = 12,
                  Size = PetSize.Small, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-20) },
        new Pet { Id = 7, Name = "Daisy", Species = Species.Dog, Breed = "Beagle Mix", AgeInMonths = 6,
                  Size = PetSize.Small, Status = ListingStatus.Available, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) }
    };

    public async Task<IReadOnlyList<Pet>> GetAllAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        return _pets.ToList();
    }

    public async Task<IReadOnlyList<Pet>> GetAvailablePetsAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        return _pets.Where(p => p.IsBrowsable()).ToList();
    }

    public async Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
        return _pets.FirstOrDefault(p => p.Id == id);
    }
}