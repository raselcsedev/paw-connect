using PawConnect.Api.Application.DTOs.Moderation;

namespace PawConnect.Api.Application.Interfaces;

public interface IReportService
{
    Task<ReportDto> SubmitAsync(
        int petId, CreateReportRequest request, CurrentUser reporter, CancellationToken cancellationToken);
}