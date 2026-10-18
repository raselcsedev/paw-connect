using PawConnect.Api.Application.DTOs.Applications;

namespace PawConnect.Api.Application.DTOs.Dashboard;

public record ApplicantDashboardDto(
    int TotalApplications,
    int ActiveApplications,
    IReadOnlyDictionary<string, int> ByStatus,
    int UnreadNotifications,
    IReadOnlyList<ApplicationSummaryDto> Recent);

public record RescuerDashboardDto(
    int TotalPets,
    IReadOnlyDictionary<string, int> PetsByStatus,
    int TotalApplications,
    IReadOnlyDictionary<string, int> ApplicationsByStatus,
    int AwaitingReview,
    int AwaitingCompletion,
    int CompletedAdoptions,
    int UnreadNotifications,
    IReadOnlyList<IncomingApplicationDto> RecentOpen);

public record UserStatsDto(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyDictionary<string, int> ByRole,
    int NewLast7Days,
    int NewLast30Days);

public record PetStatsDto(int Total, IReadOnlyDictionary<string, int> ByStatus);

public record ApplicationStatsDto(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyDictionary<string, int> ByType,
    int CompletedAdoptions);

    public record AdminDashboardDto(
    DateTime GeneratedAtUtc,
    UserStatsDto Users,
    PetStatsDto Pets,
    ApplicationStatsDto Applications,
    int OpenReports);