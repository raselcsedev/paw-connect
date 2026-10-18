using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class ReportService : IReportService
{
    private const string DuplicateMessage = "You already have an open report for this listing.";

    private readonly AppDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReportDto> SubmitAsync(
        int petId, CreateReportRequest request, CurrentUser reporter, CancellationToken cancellationToken)
    {
        var reason = request.Reason!.Value;
        var details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim();

        if (reason == ReportReason.Other && details is null)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Details"] = new[] { "Please describe the problem when the reason is Other." }
            });
        }

        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken);

        if (pet is null || pet.Status is ListingStatus.Draft or ListingStatus.Archived || pet.IsSuspended)
        {
            throw new NotFoundException(nameof(Pet), petId);
        }

        if (pet.OwnerId == reporter.Id)
        {
            throw new ConflictException("You cannot report your own listing.");
        }

        var alreadyOpen = await _db.Reports.AnyAsync(
            r => r.PetId == petId && r.ReporterId == reporter.Id && r.OpenSlot != null,
            cancellationToken);

        if (alreadyOpen)
        {
            throw new ConflictException(DuplicateMessage);
        }

        var report = new Report
        {
            PetId = petId,
            ReporterId = reporter.Id,
            Reason = reason,
            Details = details
        };

        _db.Reports.Add(report);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            throw new ConflictException(DuplicateMessage);
        }

        _logger.LogInformation(
            "Report {ReportId} ({Reason}) filed by {UserId} against pet {PetId}",
            report.Id, reason, reporter.Id, petId);

        return new ReportDto(report.Id, report.PetId, report.Reason, report.Status, report.CreatedAtUtc);
    }
}