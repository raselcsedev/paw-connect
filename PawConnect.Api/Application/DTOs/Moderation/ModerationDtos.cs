using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Moderation;

public class CreateReportRequest
{
    [Required, EnumDataType(typeof(ReportReason))]
    public ReportReason? Reason { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }
}

public class AdminReportsQuery
{
    [EnumDataType(typeof(ReportStatus))]
    public ReportStatus? Status { get; set; }

    [EnumDataType(typeof(ReportReason))]
    public ReportReason? Reason { get; set; }

    [Range(1, int.MaxValue)]
    public int? PetId { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class ReviewReportRequest
{
    [Required, EnumDataType(typeof(ReportStatus))]
    public ReportStatus? Decision { get; set; }

    [EnumDataType(typeof(ModerationAction))]
    public ModerationAction Action { get; set; } = ModerationAction.None;

    [Required, StringLength(300, MinimumLength = 5)]
    public string Note { get; set; } = string.Empty;
}

public class AdminPetsQuery
{
    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus? Status { get; set; }

    public bool? Suspended { get; set; }

    [StringLength(100)]
    public string? Q { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class ModeratePetRequest
{
    [Required, EnumDataType(typeof(ModerationAction))]
    public ModerationAction? Action { get; set; }

    [Required, StringLength(300, MinimumLength = 5)]
    public string Note { get; set; } = string.Empty;
}

public record ReportDto(int Id, int PetId, ReportReason Reason, ReportStatus Status, DateTime CreatedAtUtc);

public record AdminReportSummaryDto(
    int Id,
    int PetId,
    string PetName,
    ReportReason Reason,
    ReportStatus Status,
    string ReporterName,
    DateTime CreatedAtUtc);

public record AdminReportDto(
    int Id,
    int PetId,
    string PetName,
    ListingStatus PetStatus,
    bool PetIsSuspended,
    string OwnerName,
    string ReporterName,
    ReportReason Reason,
    string? Details,
    ReportStatus Status,
    DateTime CreatedAtUtc,
    string? ReviewedBy,
    DateTime? ReviewedAtUtc,
    string? ResolutionNote,
    int OtherOpenReportsForPet);

public record AdminPetDto(
    int Id,
    string Name,
    Species Species,
    ListingStatus Status,
    bool IsSuspended,
    string? ModerationNote,
    string OwnerName,
    int OpenReports,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);