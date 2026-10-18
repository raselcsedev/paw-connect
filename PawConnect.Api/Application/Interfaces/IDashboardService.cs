using PawConnect.Api.Application.DTOs.Dashboard;

namespace PawConnect.Api.Application.Interfaces;

public interface IDashboardService
{
    Task<ApplicantDashboardDto> GetApplicantAsync(Guid userId, CancellationToken cancellationToken);
    Task<RescuerDashboardDto> GetRescuerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<AdminDashboardDto> GetAdminAsync(CancellationToken cancellationToken);
}