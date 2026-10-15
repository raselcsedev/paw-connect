using Microsoft.AspNetCore.Identity;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}