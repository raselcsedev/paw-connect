using System.Security.Claims;
using PawConnect.Api.Application;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Identity;

namespace PawConnect.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal Signed(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));

    [Fact]
    public void GetUserId_ReadsTheSubClaim()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, Signed(new Claim("sub", id.ToString())).GetUserId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void GetUserId_MissingOrMalformedSub_IsAnAuthenticationFailure(string? value)
    {
        var principal = value is null ? Signed() : Signed(new Claim("sub", value));

        Assert.Throws<AuthenticationFailedException>(() => principal.GetUserId());
    }

    [Fact]
    public void ToCurrentUser_ReadsTheIdAndTheRoleFromTheToken()
    {
        var id = Guid.NewGuid();

        var user = Signed(new Claim("sub", id.ToString()), new Claim("role", "Rescuer")).ToCurrentUser();

        Assert.Equal(new CurrentUser(id, "Rescuer"), user);
        Assert.False(user.IsAdmin);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("admin", false)]
    [InlineData("Rescuer", false)]
    [InlineData("", false)]
    public void IsAdmin_IsAnExactRoleMatch(string role, bool expected)
    {
        Assert.Equal(expected, new CurrentUser(Guid.NewGuid(), role).IsAdmin);
    }

    [Fact]
    public void ToCurrentUserOrNull_ForAnAnonymousCaller_IsNull()
    {
        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity()).ToCurrentUserOrNull());
    }

    [Fact]
    public void ToCurrentUserOrNull_ForASignedInCaller_ReturnsTheUser()
    {
        var id = Guid.NewGuid();

        var user = Signed(new Claim("sub", id.ToString()), new Claim("role", "Adopter")).ToCurrentUserOrNull();

        Assert.NotNull(user);
        Assert.Equal(id, user.Id);
    }
}