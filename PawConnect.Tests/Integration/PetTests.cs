using System.Net;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class PetTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_StartsAsADraft_OwnedByTheCaller_WhateverTheBodyClaims()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var other = await factory.RegisterAsync("Rescuer");
        using var client = factory.As(rescuer);

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/pets", new
        {
            name = $"Pet {Guid.NewGuid():N}",
            species = "Dog",
            ageInMonths = 12,
            size = "Small",
            availabilityType = "Adoption",
            city = "Dhaka",
            id = 999999,
            status = "Adopted",
            ownerId = other.Id,
            isSuspended = true,
            moderationNote = "forged",
        });

        await response.ShouldBe(HttpStatusCode.Created);
        var pet = await response.ReadAsync<PetDto>();

        Assert.NotEqual(999999, pet.Id);
        Assert.Equal(ListingStatus.Draft, pet.Status);
        Assert.False(pet.IsSuspended);
        Assert.Null(pet.ModerationNote);
        Assert.True(pet.IsOwner);

        // The other rescuer cannot see this draft, so the forged ownerId was ignored.
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(other, pet.Id));
    }

    [Fact]
    public async Task Create_WithAnIncompleteBody_ListsEveryMissingField()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        using var client = factory.As(rescuer);

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/pets", new { name = "A" });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationAsync();

        foreach (var field in new[] { "Name", "Species", "Size", "AvailabilityType", "City" })
        {
            Assert.True(problem.HasErrorFor(field), $"Expected a validation error for {field}.");
        }
    }

    [Fact]
    public async Task ListingVisibility_FollowsTheLifecycle()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateDraftPetAsync(owner);

        // Draft: only the owner can see it.
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.Equal(HttpStatusCode.OK, await factory.GetPetStatusAsync(owner, pet.Id));
        Assert.DoesNotContain(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);

        // Published: everyone sees it, and search finds it.
        await factory.PublishPetAsync(owner, pet.Id);
        Assert.Equal(HttpStatusCode.OK, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.Contains(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);

        // Paused: reachable by link, but not listed.
        await (await factory.SetAvailabilityRawAsync(owner, pet.Id, ListingStatus.Unavailable)).ShouldBe(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.OK, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.DoesNotContain(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);

        // Archived: gone for the public, still visible to the owner.
        await (await factory.ArchiveRawAsync(owner, pet.Id)).ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.Equal(ListingStatus.Archived, (await factory.GetPetAsync(owner, pet.Id)).Status);
    }

    [Fact]
    public async Task Update_ByTheOwner_Works_TrimsInput_AndNeverChangesTheStatus()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(owner);

        var change = PetRequest(name: $"  {pet.Name}  ");
        change.City = "  Chattogram ";
        change.Description = "  Updated description  ";

        using var client = factory.As(owner);
        var response = await client.SendJsonAsync(HttpMethod.Put, $"/api/pets/{pet.Id}", change);
        await response.ShouldBe(HttpStatusCode.OK);

        var updated = await response.ReadAsync<PetDto>();
        Assert.Equal(pet.Name, updated.Name);
        Assert.Equal("Chattogram", updated.City);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(ListingStatus.Available, updated.Status);
    }

    [Fact]
    public async Task OtherUsers_CannotEditSomeoneElsesListing()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var intruder = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();
        var published = await factory.CreateAvailablePetAsync(owner);
        var draft = await factory.CreateDraftPetAsync(owner);
        var change = PetRequest(name: "Hijacked");

        async Task<HttpStatusCode> Put(TestUser? user, int id)
        {
            using var client = factory.As(user);
            return (await client.SendJsonAsync(HttpMethod.Put, $"/api/pets/{id}", change)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.Forbidden, await Put(intruder, published.Id));    // public, so 403
        Assert.Equal(HttpStatusCode.NotFound, await Put(intruder, draft.Id));         // hidden, so its existence is not revealed
        Assert.Equal(HttpStatusCode.Forbidden, await Put(adopter, published.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, await Put(null, published.Id));

        Assert.NotEqual("Hijacked", (await factory.GetPetAsync(null, published.Id)).Name);
    }

    [Fact]
    public async Task AnArchivedListing_CannotBeEdited()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(owner);
        await (await factory.ArchiveRawAsync(owner, pet.Id)).ShouldBe(HttpStatusCode.NoContent);

        using var client = factory.As(owner);
        var response = await client.SendJsonAsync(HttpMethod.Put, $"/api/pets/{pet.Id}", PetRequest());

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Availability_CannotBeSetToAdoptedOrArchived_Directly()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(owner);

        await (await factory.SetAvailabilityRawAsync(owner, pet.Id, ListingStatus.Adopted)).ShouldBe(HttpStatusCode.Conflict);
        await (await factory.SetAvailabilityRawAsync(owner, pet.Id, ListingStatus.Archived)).ShouldBe(HttpStatusCode.Conflict);

        Assert.Equal(ListingStatus.Available, (await factory.GetPetAsync(null, pet.Id)).Status);
    }

    [Fact]
    public async Task AnAdmin_CannotPublishOrArchiveSomeoneElsesListing_ModerationIsForThat()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var draft = await factory.CreateDraftPetAsync(owner);
        var published = await factory.CreateAvailablePetAsync(owner);

        await (await factory.SetAvailabilityRawAsync(admin, draft.Id, ListingStatus.Available)).ShouldBe(HttpStatusCode.Forbidden);
        await (await factory.ArchiveRawAsync(admin, published.Id)).ShouldBe(HttpStatusCode.Forbidden);

        Assert.Equal(ListingStatus.Draft, (await factory.GetPetAsync(owner, draft.Id)).Status);
        Assert.Equal(ListingStatus.Available, (await factory.GetPetAsync(null, published.Id)).Status);
    }

    [Fact]
    public async Task Archiving_IsIdempotent_ForTheOwner_ButNotForOtherRescuers()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var intruder = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(owner);

        await (await factory.ArchiveRawAsync(intruder, pet.Id)).ShouldBe(HttpStatusCode.Forbidden);
        await (await factory.ArchiveRawAsync(owner, pet.Id)).ShouldBe(HttpStatusCode.NoContent);
        await (await factory.ArchiveRawAsync(owner, pet.Id)).ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Search_FiltersAreAppliedTogether_AndCaseInsensitively()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var token = Guid.NewGuid().ToString("N");

        var dog = PetRequest(AvailabilityType.Adoption, $"Dog {token}");
        dog.Size = PetSize.Small;
        dog.AgeInMonths = 6;

        var cat = PetRequest(AvailabilityType.Foster, $"Cat {token}");
        cat.Species = Species.Cat;
        cat.Size = PetSize.Large;
        cat.AgeInMonths = 60;

        foreach (var request in new[] { dog, cat })
        {
            var draft = await factory.CreateDraftPetAsync(owner, request);
            await factory.PublishPetAsync(owner, draft.Id);
        }

        Assert.Equal(2, (await factory.SearchAsync(token)).Count);
        Assert.Equal(2, (await factory.SearchAsync(token.ToUpperInvariant())).Count);

        Assert.Equal([$"Cat {token}"], (await factory.SearchAsync(token, "&species=Cat")).Select(p => p.Name));
        Assert.Equal([$"Dog {token}"], (await factory.SearchAsync(token, "&size=Small")).Select(p => p.Name));
        Assert.Equal([$"Cat {token}"], (await factory.SearchAsync(token, "&availabilityType=Foster")).Select(p => p.Name));
        Assert.Equal([$"Dog {token}"], (await factory.SearchAsync(token, "&availabilityType=Adoption")).Select(p => p.Name));
        Assert.Equal([$"Cat {token}"], (await factory.SearchAsync(token, "&minAgeMonths=12")).Select(p => p.Name));
        Assert.Equal([$"Dog {token}"], (await factory.SearchAsync(token, "&maxAgeMonths=12")).Select(p => p.Name));
        Assert.Empty(await factory.SearchAsync(token, "&species=Cat&size=Small"));
    }

    [Theory]
    [InlineData("species=Dragon")]
    [InlineData("size=Huge")]
    [InlineData("sort=Sideways")]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=51")]
    [InlineData("minAgeMonths=-1")]
    [InlineData("minAgeMonths=50&maxAgeMonths=10")]
    public async Task Search_WithInvalidParameters_IsRefused(string query)
    {
        using var client = factory.CreateApiClient();

        await (await client.GetAsync($"/api/pets?{query}", Ct)).ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_BeyondTheLastPage_IsAnEmptyPage_NotAnError()
    {
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync($"/api/pets?q={Guid.NewGuid():N}&page=99", Ct);
        await response.ShouldBe(HttpStatusCode.OK);

        var page = await response.ReadAsync<PagedResult<PetSummaryDto>>();
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Mine_ListsOnlyTheCallersListings_IncludingDraftsAndArchived()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var other = await factory.RegisterAsync("Rescuer");

        var draft = await factory.CreateDraftPetAsync(owner);
        var live = await factory.CreateAvailablePetAsync(owner);
        var archived = await factory.CreateAvailablePetAsync(owner);
        await factory.ArchiveRawAsync(owner, archived.Id);
        var foreign = await factory.CreateDraftPetAsync(other);

        using var client = factory.As(owner);
        var response = await client.GetAsync("/api/pets/mine?pageSize=50", Ct);
        await response.ShouldBe(HttpStatusCode.OK);

        var ids = (await response.ReadAsync<PagedResult<PetDto>>()).Items.Select(p => p.Id).ToHashSet();
        Assert.Equal([draft.Id, live.Id, archived.Id], ids.Order());
        Assert.DoesNotContain(foreign.Id, ids);

        var onlyDrafts = await (await client.GetAsync("/api/pets/mine?status=Draft", Ct)).ReadAsync<PagedResult<PetDto>>();
        Assert.Equal([draft.Id], onlyDrafts.Items.Select(p => p.Id));
    }
}