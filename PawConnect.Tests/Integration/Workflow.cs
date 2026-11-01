using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Notifications;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Infrastructure.Data;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public static class Workflow
{
    public static AppDbContext Db(this IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    /* ---------- pets ---------- */

    public static async Task<HttpStatusCode> GetPetStatusAsync(this ApiFactory factory, TestUser? viewer, int petId)
    {
        using var client = factory.As(viewer);
        return (await client.GetAsync($"/api/pets/{petId}", Ct)).StatusCode;
    }

    public static async Task<PetDto> GetPetAsync(this ApiFactory factory, TestUser? viewer, int petId)
    {
        using var client = factory.As(viewer);
        var response = await client.GetAsync($"/api/pets/{petId}", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<PetDto>();
    }

    public static async Task<HttpResponseMessage> SetAvailabilityRawAsync(
        this ApiFactory factory, TestUser? user, int petId, ListingStatus status)
    {
        using var client = factory.As(user);
        return await client.SendJsonAsync(
            HttpMethod.Patch, $"/api/pets/{petId}/availability", new ChangeAvailabilityRequest { Status = status });
    }

    public static async Task<HttpResponseMessage> ArchiveRawAsync(this ApiFactory factory, TestUser? user, int petId)
    {
        using var client = factory.As(user);
        return await client.DeleteAsync($"/api/pets/{petId}", Ct);
    }

    /// <summary>Public search by a unique name or token. Uses the real endpoint, so it reflects what visitors see.</summary>
    public static async Task<List<PetSummaryDto>> SearchAsync(this ApiFactory factory, string query, string extra = "")
    {
        using var client = factory.CreateApiClient();
        var response = await client.GetAsync($"/api/pets?q={Uri.EscapeDataString(query)}&pageSize=50{extra}", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return (await response.ReadAsync<PagedResult<PetSummaryDto>>()).Items.ToList();
    }

    /* ---------- applications ---------- */

    public static CreateApplicationRequest ApplicationRequest(
        int petId, ApplicationType type = ApplicationType.Adoption, string? household = null) => new()
    {
        PetId = petId,
        Type = type,
        LivingArrangement = LivingArrangement.House,
        HouseholdInfo = household ?? "Two adults and a quiet garden.",
        CarePlan = "Daily walks, regular vet visits and plenty of company.",
        ReasonForApplying = "We have the time, space and love for a companion.",
        FosterStartDate = type == ApplicationType.Foster ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7) : null,
        FosterDurationWeeks = type == ApplicationType.Foster ? 4 : null,
    };

    public static async Task<HttpResponseMessage> SubmitRawAsync(
        this ApiFactory factory, TestUser? adopter, CreateApplicationRequest request)
    {
        using var client = factory.As(adopter);
        return await client.SendJsonAsync(HttpMethod.Post, "/api/applications", request);
    }

    public static async Task<ApplicationDto> SubmitAsync(
        this ApiFactory factory, TestUser adopter, int petId, ApplicationType type = ApplicationType.Adoption)
    {
        var response = await factory.SubmitRawAsync(adopter, ApplicationRequest(petId, type));
        await response.ShouldBe(HttpStatusCode.Created);
        return await response.ReadAsync<ApplicationDto>();
    }

    public static async Task<HttpStatusCode> GetApplicationStatusAsync(this ApiFactory factory, TestUser? viewer, int id)
    {
        using var client = factory.As(viewer);
        return (await client.GetAsync($"/api/applications/{id}", Ct)).StatusCode;
    }

    public static async Task<ApplicationDto> GetApplicationAsync(this ApiFactory factory, TestUser viewer, int id)
    {
        using var client = factory.As(viewer);
        var response = await client.GetAsync($"/api/applications/{id}", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<ApplicationDto>();
    }

    public static async Task<HttpResponseMessage> ChangeStatusRawAsync(
        this ApiFactory factory, TestUser? owner, int applicationId, ApplicationStatus status, int version, string? note = null)
    {
        using var client = factory.As(owner);
        return await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/applications/{applicationId}/status",
            new UpdateApplicationStatusRequest { Status = status, Version = version, Note = note });
    }

    public static async Task<ApplicationDto> ChangeStatusAsync(
        this ApiFactory factory, TestUser owner, int applicationId, ApplicationStatus status, int version, string? note = null)
    {
        var response = await factory.ChangeStatusRawAsync(owner, applicationId, status, version, note);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<ApplicationDto>();
    }

    public static async Task<HttpResponseMessage> CompleteRawAsync(
        this ApiFactory factory, TestUser? owner, int applicationId, int version)
    {
        using var client = factory.As(owner);
        return await client.SendJsonAsync(
            HttpMethod.Post,
            $"/api/applications/{applicationId}/complete-adoption",
            new CompleteAdoptionRequest { Version = version });
    }

    public static async Task<HttpResponseMessage> WithdrawRawAsync(this ApiFactory factory, TestUser? user, int applicationId)
    {
        using var client = factory.As(user);
        return await client.SendJsonAsync(HttpMethod.Post, $"/api/applications/{applicationId}/withdraw");
    }

    /* ---------- notifications ---------- */

    public static async Task<PagedResult<NotificationDto>> NotificationsAsync(this ApiFactory factory, TestUser user)
    {
        using var client = factory.As(user);
        var response = await client.GetAsync("/api/notifications?pageSize=50", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return await response.ReadAsync<PagedResult<NotificationDto>>();
    }

    public static async Task<int> UnreadAsync(this ApiFactory factory, TestUser user)
    {
        using var client = factory.As(user);
        var response = await client.GetAsync("/api/notifications/unread-count", Ct);
        await response.ShouldBe(HttpStatusCode.OK);
        return (await response.ReadAsync<UnreadCountDto>()).Count;
    }
}