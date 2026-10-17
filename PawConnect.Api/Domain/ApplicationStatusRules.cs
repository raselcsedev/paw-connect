using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Domain;

public static class ApplicationStatusRules
{
    public static bool IsActive(ApplicationStatus status) =>
        status is ApplicationStatus.Pending
            or ApplicationStatus.UnderReview
            or ApplicationStatus.Approved;
}