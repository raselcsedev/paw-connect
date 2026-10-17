using System.Security.Claims;
using PawConnect.Api.Application;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Infrastructure.Identity;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var id)
            ? id
            : throw new AuthenticationFailedException("The access token is not valid.");
    }

    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal) =>
        new(principal.GetUserId(), principal.FindFirst("role")?.Value ?? string.Empty);

    public static CurrentUser? ToCurrentUserOrNull(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true ? principal.ToCurrentUser() : null;
}