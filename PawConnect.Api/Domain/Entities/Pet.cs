using PawConnect.Api.Domain.Enums;

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
}