using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Domain.Entities;

public class Pet
{
    public int Id { get; set; }

    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public Species Species { get; set; }
    public string? Breed { get; set; }
    public int AgeInMonths { get; set; }
    public PetSize Size { get; set; }
    public PetSex Sex { get; set; }
    public string? Description { get; set; }
    public string? HealthStatus { get; set; }
    public bool IsVaccinated { get; set; }
    public bool IsSpayedNeutered { get; set; }
    public string? SpecialCareRequirements { get; set; }
    public AvailabilityType AvailabilityType { get; set; }
    public string? Requirements { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Area { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;

    public bool IsSuspended { get; set; }
    public string? ModerationNote { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<PetPhoto> Photos { get; set; } = new();

    public bool IsBrowsable() => Status == ListingStatus.Available && !IsSuspended;

    public void EnsureEditable()
    {
        if (Status is ListingStatus.Adopted or ListingStatus.Archived)
        {
            throw new ConflictException($"A pet that is {Status} cannot be edited.");
        }
    }

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

        if (IsSuspended && newStatus == ListingStatus.Available)
        {
            throw new ConflictException("This listing is suspended by moderation and cannot be published.");
        }

        Status = newStatus;
    }

    public void Archive()
    {
        Status = ListingStatus.Archived;
    }

    public void MarkAdopted()
    {
        if (Status == ListingStatus.Adopted)
        {
            throw new ConflictException("This pet has already been adopted.");
        }

        if (Status is ListingStatus.Draft or ListingStatus.Archived)
        {
            throw new ConflictException($"A {Status} listing cannot be marked as adopted.");
        }

        Status = ListingStatus.Adopted;
    }

    public void Suspend(string note)
    {
        if (Status is ListingStatus.Adopted or ListingStatus.Archived)
        {
            throw new ConflictException($"A pet that is {Status} cannot be suspended. Use archive instead.");
        }

        if (IsSuspended)
        {
            throw new ConflictException("This listing is already suspended.");
        }

        Status = ListingStatus.Unavailable;
        IsSuspended = true;
        ModerationNote = note;
    }

    public void RestoreFromSuspension()
    {
        if (!IsSuspended)
        {
            throw new ConflictException("This listing is not suspended.");
        }

        IsSuspended = false;
        ModerationNote = null;
    }

    public void ArchiveByModeration(string note)
    {
        if (Status == ListingStatus.Archived)
        {
            throw new ConflictException("This listing is already archived.");
        }

        Status = ListingStatus.Archived;
        IsSuspended = false;
        ModerationNote = note;
    }
}