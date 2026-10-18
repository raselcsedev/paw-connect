using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Domain.Entities;

public class Report
{
    public int Id { get; set; }

    public int PetId { get; set; }
    public Pet Pet { get; set; } = null!;

    public Guid ReporterId { get; set; }
    public ApplicationUser Reporter { get; set; } = null!;

    public ReportReason Reason { get; set; }
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public int? OpenSlot { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ResolutionNote { get; set; }

    public void Review(ReportStatus decision, Guid reviewerId, string note)
    {
        if (Status != ReportStatus.Open)
        {
            throw new ConflictException("This report has already been reviewed.");
        }

        if (decision == ReportStatus.Open)
        {
            throw new ArgumentException("A review must resolve or dismiss the report.", nameof(decision));
        }

        Status = decision;
        OpenSlot = null;
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        ResolutionNote = note;
    }
}