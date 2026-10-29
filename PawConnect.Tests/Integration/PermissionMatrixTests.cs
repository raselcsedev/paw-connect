using System.Text;
using PawConnect.Api.Application.DTOs.Pets;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class PermissionMatrixTests(ApiFactory factory)
{
    // Authorization runs before model validation, so the same empty body shows both outcomes:
    // 401 or 403 when access is refused, 400 when access is granted but the body is invalid.
    [Theory]
    // public
    [InlineData("GET", "/api/health", "Anonymous", 200)]
    [InlineData("GET", "/api/pets", "Anonymous", 200)]
    [InlineData("GET", "/api/pets", "Adopter", 200)]
    // any signed-in user
    [InlineData("GET", "/api/auth/me", "Anonymous", 401)]
    [InlineData("GET", "/api/auth/me", "Adopter", 200)]
    [InlineData("GET", "/api/auth/me", "Rescuer", 200)]
    [InlineData("GET", "/api/auth/me", "Admin", 200)]
    [InlineData("GET", "/api/notifications", "Anonymous", 401)]
    [InlineData("GET", "/api/notifications", "Adopter", 200)]
    [InlineData("GET", "/api/notifications", "Rescuer", 200)]
    [InlineData("GET", "/api/notifications", "Admin", 200)]
    [InlineData("GET", "/api/notifications/unread-count", "Anonymous", 401)]
    [InlineData("GET", "/api/notifications/unread-count", "Adopter", 200)]
    [InlineData("PATCH", "/api/account/profile", "Anonymous", 401)]
    [InlineData("PATCH", "/api/account/profile", "Adopter", 400)]
    [InlineData("POST", "/api/pets/1/reports", "Anonymous", 401)]
    [InlineData("POST", "/api/pets/1/reports", "Adopter", 400)]
    // rescuer only
    [InlineData("POST", "/api/pets", "Anonymous", 401)]
    [InlineData("POST", "/api/pets", "Adopter", 403)]
    [InlineData("POST", "/api/pets", "Admin", 403)]
    [InlineData("POST", "/api/pets", "Rescuer", 400)]
    [InlineData("GET", "/api/pets/mine", "Anonymous", 401)]
    [InlineData("GET", "/api/pets/mine", "Adopter", 403)]
    [InlineData("GET", "/api/pets/mine", "Admin", 403)]
    [InlineData("GET", "/api/pets/mine", "Rescuer", 200)]
    [InlineData("GET", "/api/applications/incoming", "Anonymous", 401)]
    [InlineData("GET", "/api/applications/incoming", "Adopter", 403)]
    [InlineData("GET", "/api/applications/incoming", "Admin", 403)]
    [InlineData("GET", "/api/applications/incoming", "Rescuer", 200)]
    [InlineData("GET", "/api/dashboard/rescuer", "Anonymous", 401)]
    [InlineData("GET", "/api/dashboard/rescuer", "Adopter", 403)]
    [InlineData("GET", "/api/dashboard/rescuer", "Admin", 403)]
    [InlineData("GET", "/api/dashboard/rescuer", "Rescuer", 200)]
    // adopter only
    [InlineData("GET", "/api/applications/my", "Anonymous", 401)]
    [InlineData("GET", "/api/applications/my", "Rescuer", 403)]
    [InlineData("GET", "/api/applications/my", "Admin", 403)]
    [InlineData("GET", "/api/applications/my", "Adopter", 200)]
    [InlineData("POST", "/api/applications", "Anonymous", 401)]
    [InlineData("POST", "/api/applications", "Rescuer", 403)]
    [InlineData("POST", "/api/applications", "Admin", 403)]
    [InlineData("POST", "/api/applications", "Adopter", 400)]
    [InlineData("GET", "/api/dashboard/applicant", "Anonymous", 401)]
    [InlineData("GET", "/api/dashboard/applicant", "Rescuer", 403)]
    [InlineData("GET", "/api/dashboard/applicant", "Admin", 403)]
    [InlineData("GET", "/api/dashboard/applicant", "Adopter", 200)]
    // admin only
    [InlineData("GET", "/api/dashboard/admin", "Anonymous", 401)]
    [InlineData("GET", "/api/dashboard/admin", "Adopter", 403)]
    [InlineData("GET", "/api/dashboard/admin", "Rescuer", 403)]
    [InlineData("GET", "/api/dashboard/admin", "Admin", 200)]
    [InlineData("GET", "/api/admin/users", "Anonymous", 401)]
    [InlineData("GET", "/api/admin/users", "Adopter", 403)]
    [InlineData("GET", "/api/admin/users", "Rescuer", 403)]
    [InlineData("GET", "/api/admin/users", "Admin", 200)]
    [InlineData("GET", "/api/admin/reports", "Anonymous", 401)]
    [InlineData("GET", "/api/admin/reports", "Adopter", 403)]
    [InlineData("GET", "/api/admin/reports", "Rescuer", 403)]
    [InlineData("GET", "/api/admin/reports", "Admin", 200)]
    [InlineData("GET", "/api/admin/pets", "Anonymous", 401)]
    [InlineData("GET", "/api/admin/pets", "Adopter", 403)]
    [InlineData("GET", "/api/admin/pets", "Rescuer", 403)]
    [InlineData("GET", "/api/admin/pets", "Admin", 200)]
    public async Task EndpointAccess_MatchesTheRolePolicy(string method, string url, string role, int expected)
    {
        TestUser? user = role == "Anonymous" ? null : await factory.SharedUserAsync(role);
        using var client = factory.As(user);

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (method is "POST" or "PATCH" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task ARescuer_CanCreateADraft_ThatOnlyTheyCanSee()
    {
        var rescuer = await factory.RegisterAsync("Rescuer");

        var draft = await factory.CreateDraftPetAsync(rescuer);

        Assert.Equal("Draft", draft.Status.ToString());
        Assert.True(draft.IsOwner);

        using var anonymous = factory.CreateApiClient();
        using var owner = factory.As(rescuer);

        await (await anonymous.GetAsync($"/api/pets/{draft.Id}", Ct)).ShouldBe(System.Net.HttpStatusCode.NotFound);

        var asOwner = await owner.GetAsync($"/api/pets/{draft.Id}", Ct);
        await asOwner.ShouldBe(System.Net.HttpStatusCode.OK);
        Assert.True((await asOwner.ReadAsync<PetDto>()).IsOwner);
    }
}