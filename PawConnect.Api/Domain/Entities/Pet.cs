using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Domain.Entities;

public class Pet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Species Species { get; set; }
    public string? Breed { get; set; }
    public int AgeInMonths { get; set; }
    public PetSize Size { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PetPhoto> Photos { get; set; } = new();

    public bool IsBrowsable() => Status == ListingStatus.Available;

    public void SetAvailability(ListingStatus newStatus)
    {
        if (Status is ListingStatus.Adopted or ListingStatus.Archived)
        {
            throw new ConflictException($"A pet that is {Status} cannot change availability.");
        }

        if (newStatus is not (ListingStatus.Draft or ListingStatus.Available or ListingStatus.Unavailable))
        {
            throw new ConflictException($"Status '{newStatus}' cannot be set through this operation.");
        }

        Status = newStatus;
    }
}