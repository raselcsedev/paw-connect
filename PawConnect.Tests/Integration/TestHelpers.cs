using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Tests.Integration;

public static class TestHelpers
{
    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    /* ---------- clients ---------- */

    public static HttpClient As(this ApiFactory factory, TestUser? user)
    {
        var client = factory.CreateApiClient();

        if (user is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        }

        return client;
    }

    public static HttpClient WithToken(this ApiFactory factory, string token)
    {
        var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static Task<HttpResponseMessage> SendJsonAsync(
        this HttpClient client, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json.Options);
        }

        return client.SendAsync(request, Ct);
    }

    /* ---------- assertions and reading ---------- */

    public static async Task<HttpResponseMessage> ShouldBe(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync(Ct);

            Assert.Fail(
                $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}." +
                $"{Environment.NewLine}Body: {body}");
        }

        return response;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json.Options, Ct);

        return value ?? throw new InvalidOperationException(
            $"The response body was empty or was not valid {typeof(T).Name} JSON.");
    }

    public static Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response) =>
        response.ReadAsync<ProblemDetails>();

    public static Task<ValidationProblemDetails> ReadValidationAsync(this HttpResponseMessage response) =>
        response.ReadAsync<ValidationProblemDetails>();

    public static bool HasErrorFor(this ValidationProblemDetails problem, string field) =>
        problem.Errors.Keys.Any(key => string.Equals(key, field, StringComparison.OrdinalIgnoreCase));

    /* ---------- users ---------- */

    public static async Task<TestUser> RegisterAsync(this ApiFactory factory, string role = "Adopter")
    {
        var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@example.com";
        var displayName = $"Test {role} {Guid.NewGuid().ToString("N")[..6]}";

        using var client = factory.CreateApiClient();

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = TestSettings.UserPassword,
            DisplayName = displayName,
            Role = role,
        });

        await response.ShouldBe(HttpStatusCode.Created);

        return TestUser.From(await response.ReadAsync<AuthResponse>(), email, TestSettings.UserPassword);
    }

    public static async Task<TestUser> LoginAsync(this ApiFactory factory, string email, string password)
    {
        using var client = factory.CreateApiClient();

        var response = await client.SendJsonAsync(
            HttpMethod.Post, "/api/auth/login", new LoginRequest { Email = email, Password = password });

        await response.ShouldBe(HttpStatusCode.OK);

        return TestUser.From(await response.ReadAsync<AuthResponse>(), email, password);
    }

    public static Task<TestUser> LoginAdminAsync(this ApiFactory factory) =>
        factory.LoginAsync(TestSettings.AdminEmail, TestSettings.AdminPassword);

    /* ---------- pets ---------- */

    public static CreatePetRequest PetRequest(AvailabilityType type = AvailabilityType.Both, string? name = null) => new()
    {
        Name = name ?? $"Pet {Guid.NewGuid():N}",
        Species = Species.Dog,
        Breed = "Mixed breed",
        AgeInMonths = 18,
        Size = PetSize.Medium,
        Sex = PetSex.Male,
        Description = "A friendly test dog.",
        IsVaccinated = true,
        AvailabilityType = type,
        City = "Dhaka",
    };

    public static async Task<PetDto> CreateDraftPetAsync(
        this ApiFactory factory, TestUser rescuer, CreatePetRequest? request = null)
    {
        using var client = factory.As(rescuer);

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/pets", request ?? PetRequest());
        await response.ShouldBe(HttpStatusCode.Created);

        return await response.ReadAsync<PetDto>();
    }

    public static async Task<PetDto> PublishPetAsync(this ApiFactory factory, TestUser rescuer, int petId)
    {
        using var client = factory.As(rescuer);

        var response = await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/pets/{petId}/availability",
            new ChangeAvailabilityRequest { Status = ListingStatus.Available });

        await response.ShouldBe(HttpStatusCode.OK);

        return await response.ReadAsync<PetDto>();
    }

    public static async Task<PetDto> CreateAvailablePetAsync(
        this ApiFactory factory, TestUser rescuer, AvailabilityType type = AvailabilityType.Both)
    {
        var draft = await factory.CreateDraftPetAsync(rescuer, PetRequest(type));
        return await factory.PublishPetAsync(rescuer, draft.Id);
    }
}