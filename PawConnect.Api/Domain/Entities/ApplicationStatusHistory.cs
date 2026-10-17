using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain.Entities;

public class ApplicationStatusHistory
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }
    public AdoptionApplication Application { get; set; } = null!;

    public ApplicationStatus? FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }
    public ApplicationUser ChangedByUser { get; set; } = null!;

    public string? Note { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}