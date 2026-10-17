using System.ComponentModel.DataAnnotations;
using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Applications;

public class CreateApplicationRequest
{
    [Range(1, int.MaxValue)]
    public int PetId { get; set; }

    [Required, EnumDataType(typeof(ApplicationType))]
    public ApplicationType? Type { get; set; }

    [Required, EnumDataType(typeof(LivingArrangement))]
    public LivingArrangement? LivingArrangement { get; set; }

    [Required, StringLength(1000, MinimumLength = 5)]
    public string HouseholdInfo { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? OtherPetsInfo { get; set; }

    [StringLength(1000)]
    public string? PreviousExperience { get; set; }

    [Required, StringLength(1500, MinimumLength = 10)]
    public string CarePlan { get; set; } = string.Empty;

    [Required, StringLength(1500, MinimumLength = 10)]
    public string ReasonForApplying { get; set; } = string.Empty;

    public DateOnly? FosterStartDate { get; set; }

    [Range(1, 52)]
    public int? FosterDurationWeeks { get; set; }

    [StringLength(1000)]
    public string? AdditionalNotes { get; set; }
}

public class MyApplicationsQuery
{
    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus? Status { get; set; }

    [EnumDataType(typeof(ApplicationType))]
    public ApplicationType? Type { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;
}

public record StatusHistoryDto(
    ApplicationStatus? FromStatus,
    ApplicationStatus ToStatus,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string? Note);

public record ApplicationSummaryDto(
    int Id,
    int PetId,
    string PetName,
    ApplicationType Type,
    ApplicationStatus Status,
    DateOnly? FosterStartDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record ApplicationDto(
    int Id,
    int PetId,
    string PetName,
    string ApplicantName,
    ApplicationType Type,
    ApplicationStatus Status,
    LivingArrangement LivingArrangement,
    string HouseholdInfo,
    string? OtherPetsInfo,
    string? PreviousExperience,
    string CarePlan,
    string ReasonForApplying,
    DateOnly? FosterStartDate,
    int? FosterDurationWeeks,
    string? AdditionalNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    int Version,
    IReadOnlyList<StatusHistoryDto> History);

public class CompleteAdoptionRequest
{
    [Required, Range(0, int.MaxValue)]
    public int? Version { get; set; }
}

public class IncomingApplicationsQuery
{
    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus? Status { get; set; }

    [EnumDataType(typeof(ApplicationType))]
    public ApplicationType? Type { get; set; }

    [Range(1, int.MaxValue)]
    public int? PetId { get; set; }

    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;
}

public class UpdateApplicationStatusRequest
{
    [Required, EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus? Status { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int? Version { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public record IncomingApplicationDto(
    int Id,
    int PetId,
    string PetName,
    string ApplicantName,
    ApplicationType Type,
    ApplicationStatus Status,
    DateOnly? FosterStartDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);