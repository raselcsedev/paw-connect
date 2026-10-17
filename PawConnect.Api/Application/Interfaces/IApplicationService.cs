using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;

namespace PawConnect.Api.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> SubmitAsync(
        CreateApplicationRequest request, CurrentUser applicant, CancellationToken cancellationToken);

    Task<PagedResult<ApplicationSummaryDto>> GetMineAsync(
        CurrentUser applicant, MyApplicationsQuery query, CancellationToken cancellationToken);

    Task<ApplicationDto> GetByIdAsync(int id, CurrentUser viewer, CancellationToken cancellationToken);
}