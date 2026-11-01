using System.Net;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.DTOs.Photos;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;
using static PawConnect.Tests.Integration.Workflow;

namespace PawConnect.Tests.Integration;

public class ModerationTests(ApiFactory factory)
{
    private async Task<HttpResponseMessage> ReportRaw(
        TestUser? user, int petId, ReportReason reason = ReportReason.Spam, string? details = null)
    {
        using var client = factory.As(user);
        return await client.SendJsonAsync(
            HttpMethod.Post, $"/api/pets/{petId}/reports", new CreateReportRequest { Reason = reason, Details = details });
    }

    private async Task<ReportDto> FileReport(TestUser user, int petId)
    {
        var response = await ReportRaw(user, petId);
        await response.ShouldBe(HttpStatusCode.Created);
        return await response.ReadAsync<ReportDto>();
    }

    private async Task<HttpResponseMessage> ReviewRaw(
        TestUser? admin, int reportId, ReportStatus decision, ModerationAction action, string note)
    {
        using var client = factory.As(admin);
        return await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/admin/reports/{reportId}",
            new ReviewReportRequest { Decision = decision, Action = action, Note = note });
    }

    private async Task<HttpResponseMessage> ModerateRaw(TestUser? user, int petId, ModerationAction action, string note)
    {
        using var client = factory.As(user);
        return await client.SendJsonAsync(
            HttpMethod.Patch, $"/api/admin/pets/{petId}/moderation", new ModeratePetRequest { Action = action, Note = note });
    }

    private async Task<AdminReportDto> GetReport(TestUser admin, int id)
    {
        using var client = factory.As(admin);
        var response = await client.GetAsync($"/api/admin/reports/{id}", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<AdminReportDto>();
    }

    /* ---------- reporting ---------- */

    [Fact]
    public async Task Reporting_HasGuardrails()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var reporter = await factory.RegisterAsync();
        var draft = await factory.CreateDraftPetAsync(owner);
        var pet = await factory.CreateAvailablePetAsync(owner);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ReportRaw(null, pet.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReportRaw(reporter, draft.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReportRaw(reporter, 2_000_000_000)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ReportRaw(owner, pet.Id)).StatusCode);

        var other = await ReportRaw(reporter, pet.Id, ReportReason.Other);
        await other.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await other.ReadValidationAsync()).HasErrorFor("Details"));

        await FileReport(reporter, pet.Id);
        var duplicate = await ReportRaw(reporter, pet.Id);
        await duplicate.ShouldBe(HttpStatusCode.Conflict);
        Assert.Contains("open report", (await duplicate.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task TheSameUserReportingFourTimesAtOnce_CreatesExactlyOneOpenReport()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var reporter = await factory.RegisterAsync();
        var pet = await factory.CreateAvailablePetAsync(owner);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => ReportRaw(reporter, pet.Id)));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(3, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    /* ---------- reviewing ---------- */

    [Fact]
    public async Task ResolvingAReportWithASuspension_HidesTheListing_LocksIt_AndTellsEveryone()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var pet = await factory.CreateAvailablePetAsync(owner);
        var reporterOne = await factory.RegisterAsync();
        var reporterTwo = await factory.RegisterAsync();
        var first = await FileReport(reporterOne, pet.Id);
        var second = await FileReport(reporterTwo, pet.Id);
        var note = $"Photos do not match the animal {Guid.NewGuid():N}.";

        var reviewed = await ReviewRaw(admin, first.Id, ReportStatus.Resolved, ModerationAction.Suspend, note);
        await reviewed.ShouldBe(HttpStatusCode.OK);
        Assert.Equal(ReportStatus.Resolved, (await reviewed.ReadAsync<AdminReportDto>()).Status);

        // The other open report about the same listing is resolved with it.
        var companion = await GetReport(admin, second.Id);
        Assert.Equal(ReportStatus.Resolved, companion.Status);
        Assert.Contains("Resolved together with report", companion.ResolutionNote);

        // The public no longer sees the listing, its photos, or search results.
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.DoesNotContain(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);

        using var anonymous = factory.CreateApiClient();
        await (await anonymous.GetAsync($"/api/pets/{pet.Id}/photos", Ct)).ShouldBe(HttpStatusCode.NotFound);
        await (await ReportRaw(await factory.RegisterAsync(), pet.Id)).ShouldBe(HttpStatusCode.NotFound);
        await (await factory.SubmitRawAsync(await factory.RegisterAsync(), ApplicationRequest(pet.Id))).ShouldBe(HttpStatusCode.NotFound);

        // The owner still sees it, with the reason, and cannot publish it again.
        var asOwner = await factory.GetPetAsync(owner, pet.Id);
        Assert.True(asOwner.IsSuspended);
        Assert.Equal(note, asOwner.ModerationNote);
        await (await factory.SetAvailabilityRawAsync(owner, pet.Id, ListingStatus.Available)).ShouldBe(HttpStatusCode.Conflict);

        // The owner gets the reason. Reporters only learn that the report was reviewed.
        var ownerNote = Assert.Single(
            (await factory.NotificationsAsync(owner)).Items, n => n.Type == NotificationType.ListingModerated);
        Assert.Contains(note, ownerNote.Message);

        foreach (var reporter in new[] { reporterOne, reporterTwo })
        {
            var told = Assert.Single(
                (await factory.NotificationsAsync(reporter)).Items, n => n.Type == NotificationType.ReportReviewed);
            Assert.Contains("action was taken", told.Message);
            Assert.DoesNotContain(note, told.Message);
        }
    }

    [Fact]
    public async Task ReviewingARejectedReport_NeedsAValidCombinationAndAUsefulNote()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var pet = await factory.CreateAvailablePetAsync(owner);
        var report = await FileReport(await factory.RegisterAsync(), pet.Id);

        foreach (var (decision, action, note) in new[]
        {
            (ReportStatus.Dismissed, ModerationAction.Suspend, "Looks fine to me."),
            (ReportStatus.Resolved, ModerationAction.Restore, "Not valid here."),
            (ReportStatus.Resolved, ModerationAction.None, "ab"),
        })
        {
            await (await ReviewRaw(admin, report.Id, decision, action, note)).ShouldBe(HttpStatusCode.BadRequest);
        }

        using var client = factory.As(admin);
        var missingDecision = await client.SendJsonAsync(
            HttpMethod.Patch, $"/api/admin/reports/{report.Id}", new { action = "None", note = "A valid note." });
        await missingDecision.ShouldBe(HttpStatusCode.BadRequest);

        Assert.Equal(ReportStatus.Open, (await GetReport(admin, report.Id)).Status);
        Assert.Equal(HttpStatusCode.OK, await factory.GetPetStatusAsync(null, pet.Id));
    }

    [Fact]
    public async Task ThreeAdminsReviewingTheSameReportAtOnce_OnlyOneDecisionIsRecorded()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var pet = await factory.CreateAvailablePetAsync(owner);
        var reporter = await factory.RegisterAsync();
        var report = await FileReport(reporter, pet.Id);

        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            ReviewRaw(admin, report.Id, ReportStatus.Dismissed, ModerationAction.None, "Checked and fine.")));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(2, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        await (await ReviewRaw(admin, report.Id, ReportStatus.Resolved, ModerationAction.None, "Second opinion."))
            .ShouldBe(HttpStatusCode.Conflict);

        // The reporter heard about it exactly once: the losing attempts rolled back their notifications.
        Assert.Single((await factory.NotificationsAsync(reporter)).Items, n => n.Type == NotificationType.ReportReviewed);
    }

    /* ---------- moderating directly ---------- */

    [Fact]
    public async Task Restoring_LiftsTheLock_ButTheOwnerDecidesWhenToPublishAgain()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var pet = await factory.CreateAvailablePetAsync(owner);

        await (await ModerateRaw(admin, pet.Id, ModerationAction.Suspend, "Under investigation.")).ShouldBe(HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(null, pet.Id));
        await (await ModerateRaw(admin, pet.Id, ModerationAction.Suspend, "Again.")).ShouldBe(HttpStatusCode.Conflict);

        var restored = await (await ModerateRaw(admin, pet.Id, ModerationAction.Restore, "Photos replaced."))
            .ReadAsync<AdminPetDto>();
        Assert.False(restored.IsSuspended);
        Assert.Equal(ListingStatus.Unavailable, restored.Status);
        Assert.Equal(HttpStatusCode.OK, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.DoesNotContain(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);

        await factory.PublishPetAsync(owner, pet.Id);
        Assert.Contains(await factory.SearchAsync(pet.Name), p => p.Id == pet.Id);
    }

    [Fact]
    public async Task ArchivingByModeration_RejectsOpenApplications_AndTellsTheApplicants()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var pet = await factory.CreateAvailablePetAsync(owner);
        var applicantUser = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(applicantUser, pet.Id);

        await (await ModerateRaw(admin, pet.Id, ModerationAction.Archive, "Fraudulent listing.")).ShouldBe(HttpStatusCode.OK);

        var closed = await factory.GetApplicationAsync(applicantUser, application.Id);
        Assert.Equal(ApplicationStatus.Rejected, closed.Status);
        Assert.Equal("Closed automatically: this listing was removed by moderation.", closed.History[^1].Note);

        Assert.Equal(HttpStatusCode.NotFound, await factory.GetPetStatusAsync(null, pet.Id));
        Assert.Equal(ListingStatus.Archived, (await factory.GetPetAsync(owner, pet.Id)).Status);
        Assert.Contains(
            (await factory.NotificationsAsync(applicantUser)).Items,
            n => n.Type == NotificationType.ApplicationStatusChanged && n.Message.Contains("Rejected"));
    }

    [Fact]
    public async Task OnlyAnAdmin_CanModerate_NotEvenTheListingOwner()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var adopter = await factory.RegisterAsync();
        var pet = await factory.CreateAvailablePetAsync(owner);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ModerateRaw(null, pet.Id, ModerationAction.Suspend, "Nope nope.")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ModerateRaw(owner, pet.Id, ModerationAction.Archive, "Nope nope.")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ModerateRaw(adopter, pet.Id, ModerationAction.Suspend, "Nope nope.")).StatusCode);

        Assert.Equal(ListingStatus.Available, (await factory.GetPetAsync(null, pet.Id)).Status);
    }

    [Fact]
    public async Task ModerationRefusesImpossibleActions()
    {
        var owner = await factory.RegisterAsync("Rescuer");
        var admin = await factory.SharedUserAsync("Admin");
        var adoptable = await factory.CreateAvailablePetAsync(owner, AvailabilityType.Adoption);

        var application = await factory.SubmitAsync(await factory.RegisterAsync(), adoptable.Id);
        var approved = await factory.ChangeStatusAsync(owner, application.Id, ApplicationStatus.Approved, application.Version);
        await factory.CompleteRawAsync(owner, application.Id, approved.Version);

        await (await ModerateRaw(admin, adoptable.Id, ModerationAction.Suspend, "Cannot suspend adopted.")).ShouldBe(HttpStatusCode.Conflict);
        await (await ModerateRaw(admin, adoptable.Id, ModerationAction.Restore, "Not suspended.")).ShouldBe(HttpStatusCode.Conflict);
        await (await ModerateRaw(admin, adoptable.Id, ModerationAction.None, "Choose an action.")).ShouldBe(HttpStatusCode.BadRequest);
        await (await ModerateRaw(admin, adoptable.Id, ModerationAction.Archive, "Fraud found later.")).ShouldBe(HttpStatusCode.OK);
        await (await ModerateRaw(admin, adoptable.Id, ModerationAction.Archive, "Again.")).ShouldBe(HttpStatusCode.Conflict);
    }
}