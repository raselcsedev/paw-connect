using System.Net;
using PawConnect.Api.Application.DTOs.Admin;
using PawConnect.Api.Application.DTOs.Auth;
using PawConnect.Api.Domain.Enums;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class AccountStatusTests(ApiFactory factory)
{
    private async Task SetStatus(TestUser admin, TestUser user, AccountStatus status)
    {
        using var client = factory.As(admin);

        var response = await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{user.Id}/status",
            new UpdateAccountStatusRequest { Status = status });

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DisablingAnAccount_StopsItsStillValidToken_Immediately()
    {
        var admin = await factory.SharedUserAsync("Admin");
        var user = await factory.RegisterAsync();

        using (var before = factory.As(user))
        {
            await (await before.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.OK);
        }

        await SetStatus(admin, user, AccountStatus.Disabled);

        using var after = factory.As(user);
        await (await after.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ADisabledAccount_CannotLogIn_OrRefresh()
    {
        var admin = await factory.SharedUserAsync("Admin");
        var user = await factory.RegisterAsync();

        await SetStatus(admin, user, AccountStatus.Disabled);

        using var client = factory.CreateApiClient();

        var login = await client.SendJsonAsync(
            HttpMethod.Post, "/api/auth/login", new LoginRequest { Email = user.Email, Password = user.Password });
        await login.ShouldBe(HttpStatusCode.Forbidden);

        var refresh = await client.SendJsonAsync(
            HttpMethod.Post, "/api/auth/refresh", new RefreshTokenRequest { RefreshToken = user.RefreshToken });
        await refresh.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReactivatingAnAccount_LetsItSignInAgain()
    {
        var admin = await factory.SharedUserAsync("Admin");
        var user = await factory.RegisterAsync();

        await SetStatus(admin, user, AccountStatus.Disabled);
        await SetStatus(admin, user, AccountStatus.Active);

        var again = await factory.LoginAsync(user.Email, user.Password);

        using var client = factory.As(again);
        await (await client.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnAdmin_CannotDisableThemselves()
    {
        var admin = await factory.LoginAdminAsync();
        using var client = factory.As(admin);

        var response = await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{admin.Id}/status",
            new UpdateAccountStatusRequest { Status = AccountStatus.Disabled });

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OnlyAnAdmin_CanChangeAccountStatus()
    {
        var adopter = await factory.SharedUserAsync("Adopter");
        var victim = await factory.RegisterAsync();

        using var client = factory.As(adopter);

        var response = await client.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{victim.Id}/status",
            new UpdateAccountStatusRequest { Status = AccountStatus.Disabled });

        await response.ShouldBe(HttpStatusCode.Forbidden);

        using var stillWorks = factory.As(victim);
        await (await stillWorks.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.OK);
    }
}