using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Mapping;

public static class ApplicationMappings
{
    public static ApplicationDto ToDto(this AdoptionApplication a) => new(
        a.Id,
        a.PetId,
        a.Pet.Name,
        a.Applicant.DisplayName,
        a.Type,
        a.Status,
        a.LivingArrangement,
        a.HouseholdInfo,
        a.OtherPetsInfo,
        a.PreviousExperience,
        a.CarePlan,
        a.ReasonForApplying,
        a.FosterStartDate,
        a.FosterDurationWeeks,
        a.AdditionalNotes,
        a.CreatedAtUtc,
        a.UpdatedAtUtc,
        a.Version,
        a.History
            .OrderBy(h => h.ChangedAtUtc)
            .ThenBy(h => h.Id)
            .Select(h => new StatusHistoryDto(
                h.FromStatus, h.ToStatus, h.ChangedByUser.DisplayName, h.ChangedAtUtc, h.Note))
            .ToList());
}