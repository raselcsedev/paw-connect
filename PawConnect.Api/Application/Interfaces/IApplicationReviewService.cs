using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;

namespace PawConnect.Api.Application.Interfaces;

public interface IApplicationReviewService
{
    Task<PagedResult<IncomingApplicationDto>> GetIncomingAsync(
        CurrentUser owner, IncomingApplicationsQuery query, CancellationToken cancellationToken);

    Task<ApplicationDto> ChangeStatusAsync(
        int id, UpdateApplicationStatusRequest request, CurrentUser owner, CancellationToken cancellationToken);

    Task<ApplicationDto> WithdrawAsync(int id, CurrentUser applicant, CancellationToken cancellationToken);
}