using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Moderation;

namespace PawConnect.Api.Application.Interfaces;

public interface IModerationService
{
    Task<PagedResult<AdminPetDto>> GetPetsAsync(AdminPetsQuery query, CancellationToken cancellationToken);

    Task<AdminPetDto> ModeratePetAsync(
        int petId, ModeratePetRequest request, CurrentUser admin, CancellationToken cancellationToken);

    Task<PagedResult<AdminReportSummaryDto>> GetReportsAsync(
        AdminReportsQuery query, CancellationToken cancellationToken);

    Task<AdminReportDto> GetReportAsync(int id, CancellationToken cancellationToken);

    Task<AdminReportDto> ReviewReportAsync(
        int id, ReviewReportRequest request, CurrentUser admin, CancellationToken cancellationToken);
}