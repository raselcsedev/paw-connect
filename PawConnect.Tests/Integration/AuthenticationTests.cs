using System.Net;
using PawConnect.Api.Application.DTOs.Auth;
using static PawConnect.Tests.Integration.TestHelpers;

namespace PawConnect.Tests.Integration;

public class AuthenticationTests(ApiFactory factory)
{
    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.SendJsonAsync(HttpMethod.Post, "/api/auth/login", new LoginRequest { Email = email, Password = password });

    private static Task<HttpResponseMessage> Refresh(HttpClient client, string refreshToken) =>
        client.SendJsonAsync(HttpMethod.Post, "/api/auth/refresh", new RefreshTokenRequest { RefreshToken = refreshToken });

    /* ---------- registration ---------- */

    [Theory]
    [InlineData("Adopter")]
    [InlineData("Rescuer")]
    public async Task Register_CreatesTheAccount_AndTheTokenWorks(string role)
    {
        var user = await factory.RegisterAsync(role);

        Assert.False(string.IsNullOrWhiteSpace(user.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(user.RefreshToken));

        using var client = factory.As(user);
        var response = await client.GetAsync("/api/auth/me", Ct);
        await response.ShouldBe(HttpStatusCode.OK);

        var me = await response.ReadAsync<UserDto>();
        Assert.Equal(user.Id, me.Id);
        Assert.Equal(role, me.Role);
        Assert.Equal(user.Email, me.Email, ignoreCase: true);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_IsRefused_EvenInDifferentCase()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/register", new RegisterRequest
        {
            Email = user.Email.ToUpperInvariant(),
            Password = TestSettings.UserPassword,
            DisplayName = "Someone Else",
            Role = "Adopter",
        });

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1234")]
    [InlineData("NOLOWERCASE1234")]
    [InlineData("NoDigitsHereAtAll")]
    public async Task Register_WithAWeakPassword_IsRefused_WithAFieldError(string password)
    {
        using var client = factory.CreateApiClient();

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/register", new RegisterRequest
        {
            Email = $"weak.{Guid.NewGuid():N}@example.com",
            Password = password,
            DisplayName = "Weak Password",
            Role = "Adopter",
        });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await response.ReadValidationAsync()).HasErrorFor("Password"));
    }

    [Fact]
    public async Task Register_AsAdmin_IsRefused_SoNobodyCanSelfPromote()
    {
        using var client = factory.CreateApiClient();

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/register", new RegisterRequest
        {
            Email = $"sneaky.{Guid.NewGuid():N}@example.com",
            Password = TestSettings.UserPassword,
            DisplayName = "Sneaky",
            Role = "Admin",
        });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.True((await response.ReadValidationAsync()).HasErrorFor("Role"));
    }

    [Fact]
    public async Task Register_IgnoresExtraFieldsLikeAStatusOrAnId()
    {
        using var client = factory.CreateApiClient();
        var email = $"extra.{Guid.NewGuid():N}@example.com";

        var response = await client.SendJsonAsync(HttpMethod.Post, "/api/auth/register", new
        {
            email,
            password = TestSettings.UserPassword,
            displayName = "Extra Fields",
            role = "Adopter",
            id = Guid.NewGuid(),
            status = "Disabled",
            isAdmin = true,
        });

        await response.ShouldBe(HttpStatusCode.Created);

        var auth = await response.ReadAsync<AuthResponse>();
        Assert.Equal("Adopter", auth.User.Role);

        using var authed = factory.WithToken(auth.AccessToken);
        await (await authed.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.OK);
    }

    /* ---------- login ---------- */

    [Fact]
    public async Task Login_WithTheRightPassword_ReturnsSessionTokens()
    {
        var user = await factory.RegisterAsync();

        var loggedIn = await factory.LoginAsync(user.Email, user.Password);

        Assert.Equal(user.Id, loggedIn.Id);
        Assert.NotEqual(user.RefreshToken, loggedIn.RefreshToken);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_GiveTheSameGenericError()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var wrongPassword = await Login(client, user.Email, "Wrong#Passw0rd1");
        var unknownEmail = await Login(client, $"nobody.{Guid.NewGuid():N}@example.com", "Wrong#Passw0rd1");

        await wrongPassword.ShouldBe(HttpStatusCode.Unauthorized);
        await unknownEmail.ShouldBe(HttpStatusCode.Unauthorized);

        var first = await wrongPassword.ReadProblemAsync();
        var second = await unknownEmail.ReadProblemAsync();

        Assert.Equal("Invalid email or password.", first.Detail);
        Assert.Equal(first.Detail, second.Detail);
    }

    [Fact]
    public async Task Login_AfterFiveFailures_LocksTheAccount_EvenForTheCorrectPassword()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await Login(client, user.Email, "Wrong#Passw0rd1");
            await failed.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var locked = await Login(client, user.Email, user.Password);

        await locked.ShouldBe(HttpStatusCode.Unauthorized);
        Assert.Contains("Too many failed attempts", (await locked.ReadProblemAsync()).Detail);
    }

    /* ---------- bearer tokens ---------- */

    [Fact]
    public async Task ProtectedEndpoint_WithoutAToken_ReturnsAProblemAnd_AChallenge()
    {
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/api/auth/me", Ct);

        await response.ShouldBe(HttpStatusCode.Unauthorized);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task AGarbageToken_IsRejected()
    {
        using var client = factory.WithToken("this.is.not-a-token");

        await (await client.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnExpiredToken_IsRejected()
    {
        var user = await factory.RegisterAsync();
        var expired = TestTokens.Signed(
            user.Id, "Adopter", DateTime.UtcNow.AddMinutes(-20), DateTime.UtcNow.AddMinutes(-10));

        using var client = factory.WithToken(expired);

        await (await client.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ATokenSignedWithTheWrongKey_IsRejected()
    {
        var user = await factory.RegisterAsync();
        var forged = TestTokens.Signed(
            user.Id, "Adopter", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10),
            key: "a-completely-different-signing-key-0123456789-xyz");

        using var client = factory.WithToken(forged);

        await (await client.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnUnsignedAlgNoneToken_ClaimingToBeAdmin_IsRejected()
    {
        var admin = await factory.SharedUserAsync("Admin");

        using var client = factory.WithToken(TestTokens.AlgNone(admin.Id, "Admin"));

        await (await client.GetAsync("/api/admin/reports", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AValidlySignedToken_ForAnAccountThatDoesNotExist_IsRejected()
    {
        using var client = factory.WithToken(TestTokens.Valid(Guid.NewGuid(), "Adopter"));

        await (await client.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
        await (await client.GetAsync("/api/applications/my", Ct)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    /* ---------- refresh tokens ---------- */

    [Fact]
    public async Task Refresh_RotatesTheTokens_AndTheNewAccessTokenWorks()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var response = await Refresh(client, user.RefreshToken);
        await response.ShouldBe(HttpStatusCode.OK);

        var renewed = await response.ReadAsync<AuthResponse>();
        Assert.NotEqual(user.RefreshToken, renewed.RefreshToken);
        Assert.NotEqual(user.AccessToken, renewed.AccessToken);

        using var authed = factory.WithToken(renewed.AccessToken);
        await (await authed.GetAsync("/api/auth/me", Ct)).ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReusingAnOldToken_IsTreatedAsTheft_AndKillsTheWholeSession()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var renewed = await (await Refresh(client, user.RefreshToken)).ReadAsync<AuthResponse>();

        var replay = await Refresh(client, user.RefreshToken);
        await replay.ShouldBe(HttpStatusCode.Unauthorized);

        var newestToken = await Refresh(client, renewed.RefreshToken);
        await newestToken.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithAnUnknownToken_GivesTheSameGenericError()
    {
        using var client = factory.CreateApiClient();

        var response = await Refresh(client, new string('x', 64));

        await response.ShouldBe(HttpStatusCode.Unauthorized);
        Assert.Equal("The refresh token is not valid.", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Refresh_RacingWithTheSameToken_OnlyOneRequestWins()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var results = await Task.WhenAll(
            Enumerable.Range(0, 4).Select(_ => Refresh(client, user.RefreshToken)));

        var statuses = results.Select(response => response.StatusCode).ToList();

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(3, statuses.Count(status => status == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken_AndIsSafeToRepeat()
    {
        var user = await factory.RegisterAsync();
        using var client = factory.CreateApiClient();

        var body = new RefreshTokenRequest { RefreshToken = user.RefreshToken };

        await (await client.SendJsonAsync(HttpMethod.Post, "/api/auth/logout", body)).ShouldBe(HttpStatusCode.NoContent);
        await (await client.SendJsonAsync(HttpMethod.Post, "/api/auth/logout", body)).ShouldBe(HttpStatusCode.NoContent);

        await (await Refresh(client, user.RefreshToken)).ShouldBe(HttpStatusCode.Unauthorized);
    }
}