using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Admin;

public class AdminUsersQuery
{
    [StringLength(100)]
    public string? Q { get; set; }

    [RegularExpression("^(Adopter|Rescuer|Admin)$", ErrorMessage = "Unknown role.")]
    public string? Role { get; set; }

    [EnumDataType(typeof(AccountStatus))]
    public AccountStatus? Status { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public record AdminUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    AccountStatus Status,
    DateTime CreatedAtUtc);