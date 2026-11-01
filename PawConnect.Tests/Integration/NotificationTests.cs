using System.Net;
using PawConnect.Api.Application.DTOs.Notifications;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;
using static PawConnect.Tests.Integration.Workflow;

namespace PawConnect.Tests.Integration;

public class NotificationTests(ApiFactory factory)
{
    [Fact]
    public async Task Submitting_NotifiesTheOwner_WithoutLeakingTheApplicantsAnswers()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        var adopter = await factory.RegisterAsync();
        var secret = $"PRIVATE-{Guid.NewGuid():N}";

        var response = await factory.SubmitRawAsync(adopter, ApplicationRequest(pet.Id, household: secret));
        await response.ShouldBe(HttpStatusCode.Created);

        var notification = Assert.Single((await factory.NotificationsAsync(rescuer)).Items);
        Assert.Equal(NotificationType.ApplicationSubmitted, notification.Type);
        Assert.Equal(pet.Id, notification.PetId);
        Assert.NotNull(notification.ApplicationId);
        Assert.False(notification.IsRead);
        Assert.Contains(adopter.DisplayName, notification.Message);
        Assert.DoesNotContain(secret, notification.Title + notification.Message);

        Assert.Equal(1, await factory.UnreadAsync(rescuer));
        Assert.Empty((await factory.NotificationsAsync(adopter)).Items);
    }

    [Fact]
    public async Task AStatusChange_NotifiesOnlyThatApplicant()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        var first = await factory.RegisterAsync();
        var second = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(first, pet.Id);
        await factory.SubmitAsync(second, pet.Id);

        await factory.ChangeStatusAsync(rescuer, application.Id, ApplicationStatus.UnderReview, application.Version);

        var mine = Assert.Single((await factory.NotificationsAsync(first)).Items);
        Assert.Equal(NotificationType.ApplicationStatusChanged, mine.Type);
        Assert.Contains("UnderReview", mine.Message);
        Assert.Contains(pet.Name, mine.Message);

        Assert.Empty((await factory.NotificationsAsync(second)).Items);
        Assert.Equal(2, (await factory.NotificationsAsync(rescuer)).Items.Count);
    }

    [Fact]
    public async Task Withdrawing_NotifiesTheOwner()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        var adopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        await (await factory.WithdrawRawAsync(adopter, application.Id)).ShouldBe(HttpStatusCode.OK);

        Assert.Contains((await factory.NotificationsAsync(rescuer)).Items, n => n.Type == NotificationType.ApplicationWithdrawn);
    }

    [Fact]
    public async Task CompletingAnAdoption_NotifiesTheWinner_AndTheRejectedApplicants()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer, AvailabilityType.Adoption);
        var winnerUser = await factory.RegisterAsync();
        var loserUser = await factory.RegisterAsync();
        var winner = await factory.SubmitAsync(winnerUser, pet.Id);
        await factory.SubmitAsync(loserUser, pet.Id);

        var approved = await factory.ChangeStatusAsync(rescuer, winner.Id, ApplicationStatus.Approved, winner.Version);
        await (await factory.CompleteRawAsync(rescuer, winner.Id, approved.Version)).ShouldBe(HttpStatusCode.OK);

        var winnerTypes = (await factory.NotificationsAsync(winnerUser)).Items.Select(n => n.Type).ToList();
        Assert.Contains(NotificationType.AdoptionCompleted, winnerTypes);
        Assert.Contains(NotificationType.ApplicationStatusChanged, winnerTypes);

        var loser = Assert.Single((await factory.NotificationsAsync(loserUser)).Items);
        Assert.Equal(NotificationType.ApplicationStatusChanged, loser.Type);
        Assert.Contains("Rejected", loser.Message);
    }

    [Fact]
    public async Task ARejectedChange_LeavesNoNotificationBehind_BecauseItRollsBackWithTheChange()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        var adopter = await factory.RegisterAsync();
        var application = await factory.SubmitAsync(adopter, pet.Id);

        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            factory.ChangeStatusRawAsync(rescuer, application.Id, ApplicationStatus.UnderReview, application.Version)));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));

        var changes = (await factory.NotificationsAsync(adopter)).Items
            .Where(n => n.Type == NotificationType.ApplicationStatusChanged);
        Assert.Single(changes);
    }

    [Fact]
    public async Task MarkingAsRead_WorksForTheOwner_IsIdempotent_AndNeverForSomeoneElse()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);
        var notification = Assert.Single((await factory.NotificationsAsync(rescuer)).Items);

        var intruder = await factory.RegisterAsync("Rescuer");
        using (var stranger = factory.As(intruder))
        {
            await (await stranger.SendJsonAsync(HttpMethod.Patch, $"/api/notifications/{notification.Id}/read"))
                .ShouldBe(HttpStatusCode.NotFound);
        }

        Assert.Equal(1, await factory.UnreadAsync(rescuer));

        using var owner = factory.As(rescuer);
        await (await owner.SendJsonAsync(HttpMethod.Patch, $"/api/notifications/{notification.Id}/read")).ShouldBe(HttpStatusCode.NoContent);
        await (await owner.SendJsonAsync(HttpMethod.Patch, $"/api/notifications/{notification.Id}/read")).ShouldBe(HttpStatusCode.NoContent);

        Assert.Equal(0, await factory.UnreadAsync(rescuer));
        Assert.True(Assert.Single((await factory.NotificationsAsync(rescuer)).Items).IsRead);
    }

    [Fact]
    public async Task ReadAll_MarksOnlyTheCallersNotifications()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");
        var other = await factory.RegisterAsync("Rescuer");
        var pet = await factory.CreateAvailablePetAsync(rescuer);
        var otherPet = await factory.CreateAvailablePetAsync(other);
        await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);
        await factory.SubmitAsync(await factory.RegisterAsync(), pet.Id);
        await factory.SubmitAsync(await factory.RegisterAsync(), otherPet.Id);

        using var client = factory.As(rescuer);
        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/notifications/read-all");
        await response.ShouldBe(HttpStatusCode.OK);

        Assert.Equal(2, (await response.ReadAsync<MarkAllReadDto>()).Updated);
        Assert.Equal(0, await factory.UnreadAsync(rescuer));
        Assert.Equal(1, await factory.UnreadAsync(other));
    }

    [Fact]
    public async Task TheNotificationList_IsPagedAndBounded()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.As(user);

        await (await client.GetAsync("/api/notifications?pageSize=1000", Ct)).ShouldBe(HttpStatusCode.BadRequest);
        await (await client.GetAsync("/api/notifications?page=0", Ct)).ShouldBe(HttpStatusCode.BadRequest);
        await (await client.GetAsync("/api/notifications?pageSize=1", Ct)).ShouldBe(HttpStatusCode.OK);
    }
}