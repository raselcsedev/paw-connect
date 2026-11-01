using System.Net;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;
using static PawConnect.Tests.Integration.Workflow;

namespace PawConnect.Tests.Integration;

public class ApplicationLifecycleTests(ApiFactory factory)
{
    private async Task<(TestUser Rescuer, PetDto Pet)> PublishedPet(AvailabilityType type = AvailabilityType.Both)
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        return (rescuer, await factory.CreateAvailablePetAsync(rescuer, type));
    }

    private static int Count(IEnumerable<HttpResponseMessage> responses, HttpStatusCode code) =>
        responses.Count(response => response.StatusCode == code);

    /* ---------- submitting ---------- */

    [Fact]
    public async Task Submit_CreatesAPendingApplication_WithTheFirstHistoryEntry()
    {
        var (_, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();

        var application = await factory.SubmitAsync(adopter, pet.Id);

        Assert.Equal(ApplicationStatus.Pending, application.Status);
        Assert.Equal(0, application.Version);
        Assert.Equal(pet.Name, application.PetName);
        Assert.Equal(adopter.DisplayName, application.ApplicantName);

        var entry = Assert.Single(application.History);
        Assert.Null(entry.FromStatus);
        Assert.Equal(ApplicationStatus.Pending, entry.ToStatus);
        Assert.Equal(adopter.DisplayName, entry.ChangedBy);
    }

    [Fact]
    public async Task Submit_Twice_IsRefused_UntilTheFirstApplicationIsWithdrawn()
    {
        var (_, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        var first = await factory.SubmitAsync(adopter, pet.Id);

        var duplicate = await factory.SubmitRawAsync(adopter, ApplicationRequest(pet.Id));
        await duplicate.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains("active application", (await duplicate.ReadProblemAsync()).Detail);

        await (await factory.WithdrawRawAsync(adopter, first.Id)).ShouldBe(HttpStatusCode.OK);

        var again = await factory.SubmitRawAsync(adopter, ApplicationRequest(pet.Id));
        await again.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Submit_RacingFourIdenticalRequests_CreatesExactlyOneApplication()
    {
        var (_, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => factory.SubmitRawAsync(adopter, ApplicationRequest(pet.Id))));

        Assert.Equal(1, Count(results, HttpStatusCode.Created));
        Assert.Equal(3, Count(results, HttpStatusCode.Conflict));

        using var client = factory.As(adopter);
        var mine = await (await client.GetAsync("/api/applications/my", Ct)).ReadAsync<PagedResult<ApplicationSummaryDto>>();
        Assert.Equal(1, mine.TotalCount);
    }

    [Fact]
    public async Task Submit_MustMatchWhatThePetIsOfferedFor()
    {
        var (_, adoptionOnly) = await PublishedPet(AvailabilityType.Adoption);
        var (_, fosterOnly) = await PublishedPet(AvailabilityType.Foster);
        var adopter = await factory.RegisterAsync();

        var foster = await factory.SubmitRawAsync(adopter, ApplicationRequest(adoptionOnly.Id, ApplicationType.Foster));
        await foster.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains("foster", (await foster.ReadProblemAsync()).Detail);

        var adoption = await factory.SubmitRawAsync(adopter, ApplicationRequest(fosterOnly.Id, ApplicationType.Adoption));
        await adoption.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains("adoption", (await adoption.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Submit_Foster_NeedsADateInTheFutureAndADuration()
    {
        var (_, pet) = await PublishedPet(AvailabilityType.Foster);
        var adopter = await factory.RegisterAsync();

        var missing = ApplicationRequest(pet.Id, ApplicationType.Foster);
        missing.FosterStartDate = null;
        missing.FosterDurationWeeks = null;

        var response = await factory.SubmitRawAsync(adopter, missing);
        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationAsync();
        Assert.True(problem.HasErrorFor("FosterStartDate"));
        Assert.True(problem.HasErrorFor("FosterDurationWeeks"));

        var past = ApplicationRequest(pet.Id, ApplicationType.Foster);
        past.FosterStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var pastResponse = await factory.SubmitRawAsync(adopter, past);
        await pastResponse.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await pastResponse.ReadValidationAsync()).HasErrorFor("FosterStartDate"));

        var valid = await factory.SubmitAsync(adopter, pet.Id, ApplicationType.Foster);
        Assert.Equal(ApplicationType.Foster, valid.Type);
        Assert.Equal(4, valid.FosterDurationWeeks);
    }

    [Fact]
    public async Task Submit_ForPetsThatAreNotOpen_IsRefusedAccordingToTheirState()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();

        var draft = await factory.CreateDraftPetAsync(rescuer);
        var paused = await factory.CreateAvailablePetAsync(rescuer);
        await (await factory.SetAvailabilityRawAsync(rescuer, paused.Id, ListingStatus.Unavailable)).ShouldBe(HttpStatusCode.OK);
        var archived = await factory.CreateAvailablePetAsync(rescuer);
        await (await factory.ArchiveRawAsync(rescuer, archived.Id)).ShouldBe(HttpStatusCode.NoContent);

        Assert.Equal(HttpStatusCode.NotFound, (await factory.SubmitRawAsync(adopter, ApplicationRequest(draft.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await factory.SubmitRawAsync(adopter, ApplicationRequest(paused.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.SubmitRawAsync(adopter, ApplicationRequest(archived.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.SubmitRawAsync(adopter, ApplicationRequest(2_000_000_000))).StatusCode);
    }

    /* ---------- who can see an application ---------- */

    [Fact]
    public async Task OnlyTheApplicantAndThePetOwner_CanReadAnApplication()
    {
        var (rescuer, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        var otherAdopter = await factory.RegisterAsync();
        var otherRescuer = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var application = await factory.SubmitAsync(adopter, pet.Id);

        Assert.Equal(HttpStatusCode.OK, await factory.GetApplicationStatusAsync(adopter, application.Id));
        Assert.Equal(HttpStatusCode.OK, await factory.GetApplicationStatusAsync(rescuer, application.Id));
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetApplicationStatusAsync(otherAdopter, application.Id));
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetApplicationStatusAsync(otherRescuer, application.Id));
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetApplicationStatusAsync(admin, application.Id));
        Assert.Equal(HttpStatusCode.Unauthorized, await factory.GetApplicationStatusAsync(null, application.Id));
    }

    [Fact]
    public async Task IncomingApplications_AreScopedToTheOwnersPets()
    {
        var (rescuer, pet) = await PublishedPet();
        var otherRescuer = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        using var owner = factory.As(rescuer);
        var own = await owner.GetAsync($"/api/applications/incoming?petId={pet.Id}", Ct);
        await own.ShouldBe(HttpStatusCode.OK);
        Assert.Contains(
            (await own.ReadAsync<PagedResult<IncomingApplicationDto>>()).Items,
            item => item.Id == application.Id && item.ApplicantName == adopter.DisplayName);

        using var other = factory.As(otherRescuer);
        await (await other.GetAsync($"/api/applications/incoming?petId={pet.Id}", Ct)).ShouldBe(HttpStatusCode.NotFound);

        var everything = await other.GetAsync("/api/applications/incoming?pageSize=50", Ct);
        await everything.ShouldBe(HttpStatusCode.OK);
        Assert.DoesNotContain(
            (await everything.ReadAsync<PagedResult<IncomingApplicationDto>>()).Items,
            item => item.Id == application.Id);
    }

    /* ---------- reviewing ---------- */

    [Fact]
    public async Task Review_AppliesTheStateMachine_WithVersionsHistoryAndNotes()
    {
        var (rescuer, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        var reviewing = await factory.ChangeStatusAsync(rescuer, application.Id, ApplicationStatus.UnderReview, application.Version);
        Assert.Equal(ApplicationStatus.UnderReview, reviewing.Status);
        Assert.Equal(application.Version + 1, reviewing.Version);
        Assert.Equal(2, reviewing.History.Count);
        Assert.Equal(rescuer.DisplayName, reviewing.History[^1].ChangedBy);

        Assert.Equal(2, (await factory.GetApplicationAsync(adopter, application.Id)).History.Count);

        // A request that carries an old version must not win.
        await (await factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.Approved, application.Version))
            .ShouldBe(HttpStatusCode.Conflict);

        // An illegal move.
        await (await factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.Pending, reviewing.Version))
            .ShouldBe(HttpStatusCode.Conflict);

        // A rejection needs a note.
        var withoutNote = await factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.Rejected, reviewing.Version);
        await withoutNote.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await withoutNote.ReadValidationAsync()).HasErrorFor("Note"));

        var rejected = await factory.ChangeStatusAsync(
            rescuer, application.Id, ApplicationStatus.Rejected, reviewing.Version, "Not a good match.");
        Assert.Equal("Not a good match.", rejected.History[^1].Note);

        // Rejected is final.
        await (await factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.Approved, rejected.Version))
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Review_IsForTheOwnerOnly()
    {
        var (_, pet) = await PublishedPet();
        var otherRescuer = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        Assert.Equal(HttpStatusCode.NotFound,
            (await factory.ChangeStatusRawAsync(otherRescuer, application.Id, ApplicationStatus.Approved, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await factory.ChangeStatusRawAsync(adopter, application.Id, ApplicationStatus.Approved, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await factory.ChangeStatusRawAsync(null, application.Id, ApplicationStatus.Approved, 0)).StatusCode);

        Assert.Equal(ApplicationStatus.Pending, (await factory.GetApplicationAsync(adopter, application.Id)).Status);
    }

    [Fact]
    public async Task OnlyOneApplicationCanBeApprovedForAPet_UntilTheApprovalIsRevoked()
    {
        var (rescuer, pet) = await PublishedPet();
        var first = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);
        var second = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        var approved = await factory.ChangeStatusAsync(rescuer, first.Id, ApplicationStatus.Approved, first.Version);

        var blocked = await factory.ChangeStatusRawAsync(rescuer, second.Id, ApplicationStatus.Approved, second.Version);
        await blocked.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains("already approved", (await blocked.ReadProblemAsync()).Detail);

        await factory.ChangeStatusAsync(rescuer, first.Id, ApplicationStatus.Rejected, approved.Version, "Changed our minds.");
        await factory.ChangeStatusAsync(rescuer, second.Id, ApplicationStatus.Approved, second.Version);
    }

    [Fact]
    public async Task ApprovingTwoDifferentApplicationsAtTheSameTime_OnlyOneWins()
    {
        var (rescuer, pet) = await PublishedPet();
        var a = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);
        var b = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        var results = await Task.WhenAll(
            factory.ChangeStatusRawAsync(rescuer, a.Id, ApplicationStatus.Approved, a.Version),
            factory.ChangeStatusRawAsync(rescuer, b.Id, ApplicationStatus.Approved, b.Version));

        Assert.Equal(1, Count(results, HttpStatusCode.OK));
        Assert.Equal(1, Count(results, HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task ThreeSimultaneousDecisions_WithTheSameVersion_OnlyOneIsApplied()
    {
        var (rescuer, pet) = await PublishedPet();
        var application = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);

        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.UnderReview, application.Version)));

        Assert.Equal(1, Count(results, HttpStatusCode.OK));
        Assert.Equal(2, Count(results, HttpStatusCode.Conflict));

        var current = await factory.GetApplicationAsync(rescuer, application.Id);
        Assert.Equal(application.Version + 1, current.Version);
        Assert.Equal(2, current.History.Count);
    }

    [Fact]
    public async Task ApprovingAFoster_NeverClosesTheListing_AndCannotBeCompleted()
    {
        var (rescuer, pet) = await PublishedPet(AvailabilityType.Foster);
        var first = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id, ApplicationType.Foster);
        var second = await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id, ApplicationType.Foster);

        var approved = await factory.ChangeStatusAsync(rescuer, first.Id, ApplicationStatus.Approved, first.Version);

        Assert.Equal(ListingStatus.Available, (await factory.GetPetAsync(null, pet.Id)).Status);
        await (await factory.CompleteRawAsync(rescuer, first.Id, approved.Version)).ShouldBe(HttpStatusCode.Conflict);
        await (await factory.ChangeStatusRawAsync(rescuer, second.Id, ApplicationStatus.Approved, second.Version))
            .ShouldBe(HttpStatusCode.Conflict);
    }

    /* ---------- withdrawing ---------- */

    [Fact]
    public async Task Withdraw_IsOnlyPossibleWhilePending_AndOnlyForTheApplicant()
    {
        var (rescuer, pet) = await PublishedPet();
        var adopter = await factory.RegisterAsync();
        var otherAdopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        Assert.Equal(HttpStatusCode.NotFound, (await factory.WithdrawRawAsync(otherAdopter, application.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await factory.WithdrawRawAsync(rescuer, application.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.WithdrawRawAsync(null, application.Id)).StatusCode);

        var withdrawn = await (await factory.WithdrawRawAsync(adopter, application.Id)).ReadAsync<ApplicationDto>();
        Assert.Equal(ApplicationStatus.Withdrawn, withdrawn.Status);
        Assert.Equal("Withdrawn by the applicant.", withdrawn.History[^1].Note);

        await (await factory.WithdrawRawAsync(adopter, application.Id)).ShouldBe(HttpStatusCode.Conflict);

        var underReview = await factory.SubmitAsync(adopter, pet.Id);
        await factory.ChangeStatusAsync(rescuer, underReview.Id, ApplicationStatus.UnderReview, underReview.Version);
        await (await factory.WithdrawRawAsync(adopter, underReview.Id)).ShouldBe(HttpStatusCode.Conflict);
    }

    /* ---------- completing an adoption ---------- */

    [Fact]
    public async Task CompletingAnAdoption_ClosesThePet_AndRejectsEveryOtherOpenApplication()
    {
        var (rescuer, pet) = await PublishedPet(AvailabilityType.Adoption);
        var winnerUser = await factory.RegisterAsync();
        var loserUser = await factory.RegisterAsync();
        var waitingUser = await factory.RegisterAsync();
        var latecomer = await factory.RegisterAsync();

        var winner = await factory.SubmitAsync(winnerUser, pet.Id);
        var loser = await factory.SubmitAsync(loserUser, pet.Id);
        var waiting = await factory.SubmitAsync(waitingUser, pet.Id);
        await factory.ChangeStatusAsync(rescuer, waiting.Id, ApplicationStatus.UnderReview, waiting.Version);

        // Not approved yet, or a stale version: refused.
        await (await factory.CompleteRawAsync(rescuer, loser.Id, loser.Version)).ShouldBe(HttpStatusCode.Conflict);
        var approved = await factory.ChangeStatusAsync(rescuer, winner.Id, ApplicationStatus.Approved, winner.Version);
        await (await factory.CompleteRawAsync(rescuer, winner.Id, winner.Version)).ShouldBe(HttpStatusCode.Conflict);

        // Only the owner may complete.
        var intruder = await factory.RegisterAsync("Rescuer");
        await (await factory.CompleteRawAsync(intruder, winner.Id, approved.Version)).ShouldBe(HttpStatusCode.NotFound);
        await (await factory.CompleteRawAsync(winnerUser, winner.Id, approved.Version)).ShouldBe(HttpStatusCode.Forbidden);

        var completed = await (await factory.CompleteRawAsync(rescuer, winner.Id, approved.Version)).ReadAsync<ApplicationDto>();
        Assert.NotNull(completed.CompletedAtUtc);
        Assert.Equal(ApplicationStatus.Approved, completed.Status);

        // The pet is closed for everyone.
        Assert.Equal(ListingStatus.Adopted, (await factory.GetPetAsync(null, pet.Id)).Status);
        Assert.DoesNotContain(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);
        await (await factory.SubmitRawAsync(latecomer, ApplicationRequest(pet.Id))).ShouldBe(HttpStatusCode.Conflict);

        // Every competing open application was rejected, with an explanation and a history entry.
        foreach (var (user, id) in new[] { (loserUser, loser.Id), (waitingUser, waiting.Id) })
        {
            var closed = await factory.GetApplicationAsync(user, id);
            Assert.Equal(ApplicationStatus.Rejected, closed.Status);
            Assert.Equal("Closed automatically: this pet has been adopted.", closed.History[^1].Note);
        }

        // A completed adoption is frozen.
        await (await factory.CompleteRawAsync(rescuer, winner.Id, completed.Version)).ShouldBe(HttpStatusCode.Conflict);
        await (await factory.ChangeStatusRawAsync(rescuer, winner.Id, ApplicationStatus.Rejected, completed.Version, "Too late."))
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Archiving_AListing_RejectsItsOpenApplications_ButNeverTouchesACompletedAdoption()
    {
        var (rescuer, pet) = await PublishedPet(AvailabilityType.Adoption);
        var openUser = await factory.RegisterAsync();
        var open = await factory.SubmitAsync(openUser, pet.Id);

        await (await factory.ArchiveRawAsync(rescuer, pet.Id)).ShouldBe(HttpStatusCode.NoContent);

        var closed = await factory.GetApplicationAsync(openUser, open.Id);
        Assert.Equal(ApplicationStatus.Rejected, closed.Status);
        Assert.Equal("Closed automatically: this listing was archived.", closed.History[^1].Note);

        // Another pet: a completed adoption survives its listing being archived.
        var (rescuer2, pet2) = await PublishedPet(AvailabilityType.Adoption);
        var adopterUser = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopterUser, pet2.Id);
        var approved = await factory.ChangeStatusAsync(rescuer2, application.Id, ApplicationStatus.Approved, application.Version);
        await (await factory.CompleteRawAsync(rescuer2, application.Id, approved.Version)).ShouldBe(HttpStatusCode.OK);

        await (await factory.ArchiveRawAsync(rescuer2, pet2.Id)).ShouldBe(HttpStatusCode.NoContent);

        var stillCompleted = await factory.GetApplicationAsync(adopterUser, application.Id);
        Assert.Equal(ApplicationStatus.Approved, stillCompleted.Status);
        Assert.NotNull(stillCompleted.CompletedAtUtc);
    }
}