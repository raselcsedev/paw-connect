using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Infrastructure.Data;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Pets.AnyAsync())
        {
            return;
        }

        var cities = new[] { "Dhaka", "Chattogram", "Sylhet" };
        var names = new[]
        {
            "Milo", "Luna", "Max", "Bella", "Charlie", "Daisy", "Rocky", "Coco",
            "Oscar", "Ruby", "Buddy", "Lily", "Simba", "Nala", "Teddy", "Mochi",
            "Pepper", "Biscuit", "Shadow", "Maple", "Ziggy", "Olive", "Ginger", "Pixel"
        };

        var pets = new List<Pet>();

        for (var i = 0; i < names.Length; i++)
        {
            var species = (Species)(i % 4);

            pets.Add(new Pet
            {
                Name = names[i],
                Species = species,
                Breed = species switch
                {
                    Species.Dog => "Mixed breed",
                    Species.Cat => "Domestic shorthair",
                    Species.Rabbit => "Holland Lop",
                    _ => "Cockatiel"
                },
                AgeInMonths = 3 + (i * 7) % 90,
                Size = (PetSize)(i % 3),
                Sex = i % 2 == 0 ? PetSex.Male : PetSex.Female,
                Description = $"{names[i]} is a friendly {species.ToString().ToLower()} looking for a loving home.",
                IsVaccinated = i % 3 != 0,
                IsSpayedNeutered = i % 2 == 0,
                AvailabilityType = (AvailabilityType)(i % 3),
                City = cities[i % cities.Length],
                Status = i % 8 == 7 ? ListingStatus.Adopted : ListingStatus.Available,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        db.Pets.AddRange(pets);
        await db.SaveChangesAsync();
    }
}