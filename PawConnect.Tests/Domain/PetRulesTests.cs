using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Tests;

public class PetRulesTests
{
    private static Pet Create(ListingStatus status) => new() { Name = "Milo", Status = status };

    [Theory]
    [InlineData(ListingStatus.Available, false, true)]
    [InlineData(ListingStatus.Available, true, false)]
    [InlineData(ListingStatus.Draft, false, false)]
    [InlineData(ListingStatus.Unavailable, false, false)]
    [InlineData(ListingStatus.Adopted, false, false)]
    [InlineData(ListingStatus.Archived, false, false)]
    public void IsBrowsable_RequiresAvailableAndNotSuspended(ListingStatus status, bool suspended, bool expected)
    {
        var pet = new Pet { Status = status, IsSuspended = suspended };

        Assert.Equal(expected, pet.IsBrowsable());
    }

    [Theory]
    [InlineData(ListingStatus.Draft, ListingStatus.Available)]
    [InlineData(ListingStatus.Available, ListingStatus.Unavailable)]
    [InlineData(ListingStatus.Unavailable, ListingStatus.Available)]
    [InlineData(ListingStatus.Available, ListingStatus.Draft)]
    public void SetAvailability_AllowedChanges_Apply(ListingStatus from, ListingStatus to)
    {
        var pet = Create(from);

        pet.SetAvailability(to);

        Assert.Equal(to, pet.Status);
    }

    [Theory]
    [InlineData(ListingStatus.Adopted)]
    [InlineData(ListingStatus.Archived)]
    public void SetAvailability_FromAFrozenStatus_IsRefused(ListingStatus frozen)
    {
        var pet = Create(frozen);

        Assert.Throws<ConflictException>(() => pet.SetAvailability(ListingStatus.Available));
        Assert.Equal(frozen, pet.Status);
    }

    [Theory]
    [InlineData(ListingStatus.Adopted)]
    [InlineData(ListingStatus.Archived)]
    public void SetAvailability_CannotReachAdoptedOrArchived(ListingStatus target)
    {
        var pet = Create(ListingStatus.Available);

        Assert.Throws<ConflictException>(() => pet.SetAvailability(target));
    }

    [Fact]
    public void Suspend_AvailablePet_HidesTheListingAndLocksIt()
    {
        var pet = Create(ListingStatus.Available);

        pet.Suspend("Photos do not match the animal.");

        Assert.Equal(ListingStatus.Unavailable, pet.Status);
        Assert.True(pet.IsSuspended);
        Assert.Equal("Photos do not match the animal.", pet.ModerationNote);
        Assert.False(pet.IsBrowsable());
    }

    [Fact]
    public void SuspendedPet_CannotBePublishedByItsOwner()
    {
        var pet = Create(ListingStatus.Available);
        pet.Suspend("Under review.");

        Assert.Throws<ConflictException>(() => pet.SetAvailability(ListingStatus.Available));
        Assert.Equal(ListingStatus.Unavailable, pet.Status);
    }

    [Fact]
    public void Restore_LiftsTheLockButLeavesThePetPaused_ThenTheOwnerCanPublish()
    {
        var pet = Create(ListingStatus.Available);
        pet.Suspend("Under review.");

        pet.RestoreFromSuspension();

        Assert.False(pet.IsSuspended);
        Assert.Null(pet.ModerationNote);
        Assert.Equal(ListingStatus.Unavailable, pet.Status);

        pet.SetAvailability(ListingStatus.Available);
        Assert.True(pet.IsBrowsable());
    }

    [Fact]
    public void Restore_WhenNotSuspended_IsRefused()
    {
        Assert.Throws<ConflictException>(() => Create(ListingStatus.Available).RestoreFromSuspension());
    }

    [Theory]
    [InlineData(ListingStatus.Adopted)]
    [InlineData(ListingStatus.Archived)]
    public void Suspend_AdoptedOrArchivedPet_IsRefused(ListingStatus status)
    {
        Assert.Throws<ConflictException>(() => Create(status).Suspend("Nope."));
    }

    [Fact]
    public void Suspend_Twice_IsRefused()
    {
        var pet = Create(ListingStatus.Available);
        pet.Suspend("First.");

        Assert.Throws<ConflictException>(() => pet.Suspend("Second."));
    }

    [Fact]
    public void ArchiveByModeration_WorksEvenOnAnAdoptedPet_AndClearsTheSuspension()
    {
        var pet = Create(ListingStatus.Adopted);
        pet.IsSuspended = true;

        pet.ArchiveByModeration("Fraudulent listing.");

        Assert.Equal(ListingStatus.Archived, pet.Status);
        Assert.False(pet.IsSuspended);
        Assert.Equal("Fraudulent listing.", pet.ModerationNote);
    }

    [Fact]
    public void ArchiveByModeration_WhenAlreadyArchived_IsRefused()
    {
        Assert.Throws<ConflictException>(() => Create(ListingStatus.Archived).ArchiveByModeration("Again."));
    }

    [Theory]
    [InlineData(ListingStatus.Available)]
    [InlineData(ListingStatus.Unavailable)]
    public void MarkAdopted_FromALiveListing_Works(ListingStatus from)
    {
        var pet = Create(from);

        pet.MarkAdopted();

        Assert.Equal(ListingStatus.Adopted, pet.Status);
    }

    [Theory]
    [InlineData(ListingStatus.Draft)]
    [InlineData(ListingStatus.Archived)]
    [InlineData(ListingStatus.Adopted)]
    public void MarkAdopted_FromDraftArchivedOrAlreadyAdopted_IsRefused(ListingStatus from)
    {
        Assert.Throws<ConflictException>(() => Create(from).MarkAdopted());
    }

    [Theory]
    [InlineData(ListingStatus.Adopted, true)]
    [InlineData(ListingStatus.Archived, true)]
    [InlineData(ListingStatus.Draft, false)]
    [InlineData(ListingStatus.Available, false)]
    [InlineData(ListingStatus.Unavailable, false)]
    public void EnsureEditable_RefusesOnlyFrozenListings(ListingStatus status, bool shouldThrow)
    {
        var pet = Create(status);

        if (shouldThrow)
        {
            Assert.Throws<ConflictException>(() => pet.EnsureEditable());
        }
        else
        {
            pet.EnsureEditable();
        }
    }
}