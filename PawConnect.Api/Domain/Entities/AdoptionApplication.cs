using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain.Entities;

public class AdoptionApplication
{
    public int Id { get; set; }

    public int PetId { get; set; }
    public Pet Pet { get; set; } = null!;

    public Guid ApplicantId { get; set; }
    public ApplicationUser Applicant { get; set; } = null!;

    public ApplicationType Type { get; set; }
    public LivingArrangement LivingArrangement { get; set; }

    public string HouseholdInfo { get; set; } = string.Empty;
    public string? OtherPetsInfo { get; set; }
    public string? PreviousExperience { get; set; }
    public string CarePlan { get; set; } = string.Empty;
    public string ReasonForApplying { get; set; } = string.Empty;

    public DateOnly? FosterStartDate { get; set; }
    public int? FosterDurationWeeks { get; set; }
    public string? AdditionalNotes { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public int? ActiveSlot { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ApplicationStatusHistory> History { get; set; } = new();

    public void RefreshActiveSlot()
    {
        ActiveSlot = ApplicationStatusRules.IsActive(Status) ? 1 : null;
    }
}