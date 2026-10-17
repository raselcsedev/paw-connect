using PawConnect.Api.Domain;

namespace PawConnect.Api.Application;

public record CurrentUser(Guid Id, string Role)
{
    public bool IsAdmin => Role == Roles.Admin;
}