using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain;

public static class ApplicationStatusRules
{
    public static bool IsActive(ApplicationStatus status) =>
        status is ApplicationStatus.Pending
            or ApplicationStatus.UnderReview
            or ApplicationStatus.Approved;

    public static bool CanTransition(ApplicationStatus from, ApplicationStatus to, ApplicationActor actor)
    {
        return actor switch
        {
            ApplicationActor.Applicant =>
                from == ApplicationStatus.Pending && to == ApplicationStatus.Withdrawn,

            ApplicationActor.PetOwner => (from, to) switch
            {
                (ApplicationStatus.Pending, ApplicationStatus.UnderReview) => true,
                (ApplicationStatus.Pending, ApplicationStatus.Approved) => true,
                (ApplicationStatus.Pending, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Approved) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.Approved, ApplicationStatus.Rejected) => true,
                _ => false
            },

            _ => false
        };
    }
}