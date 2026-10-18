using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Moderation;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class ModerationService : IModerationService
{
    private readonly AppDbContext _db;
    private readonly INotificationPublisher _notifications;
    private readonly ILogger<ModerationService> _logger;

    public ModerationService(
        AppDbContext db,
        INotificationPublisher notifications,
        ILogger<ModerationService> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<PagedResult<AdminPetDto>> GetPetsAsync(
        AdminPetsQuery parameters, CancellationToken cancellationToken)
    {
        IQueryable<Pet> query = _db.Pets.AsNoTracking();

        if (parameters.Status.HasValue)
        {
            var status = parameters.Status.Value;
            query = query.Where(p => p.Status == status);
        }

        if (parameters.Suspended.HasValue)
        {
            var suspended = parameters.Suspended.Value;
            query = query.Where(p => p.IsSuspended == suspended);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var term = parameters.Q.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Breed != null && p.Breed.Contains(term)));
        }

        query = query.OrderByDescending(p => p.UpdatedAtUtc).ThenByDescending(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectPets(query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminPetDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<AdminPetDto> ModeratePetAsync(
        int petId, ModeratePetRequest request, CurrentUser admin, CancellationToken cancellationToken)
    {
        var action = request.Action!.Value;

        if (action == ModerationAction.None)
        {
            throw Invalid("Action", "Choose Suspend, Restore or Archive.");
        }

        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == petId, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), petId);

        var note = request.Note.Trim();

        await ApplyActionAsync(pet, action, note, admin.Id, cancellationToken);
        await SaveAsync(cancellationToken);

        _logger.LogInformation(
            "AUDIT: admin {AdminId} applied {Action} to pet {PetId}", admin.Id, action, petId);

        return await ProjectPets(_db.Pets.AsNoTracking().Where(p => p.Id == petId))
            .FirstAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminReportSummaryDto>> GetReportsAsync(
        AdminReportsQuery parameters, CancellationToken cancellationToken)
    {
        IQueryable<Report> query = _db.Reports.AsNoTracking();

        if (parameters.Status.HasValue)
        {
            var status = parameters.Status.Value;
            query = query.Where(r => r.Status == status);
        }

        if (parameters.Reason.HasValue)
        {
            var reason = parameters.Reason.Value;
            query = query.Where(r => r.Reason == reason);
        }

        if (parameters.PetId.HasValue)
        {
            var petId = parameters.PetId.Value;
            query = query.Where(r => r.PetId == petId);
        }

        query = query
            .OrderBy(r => r.Status == ReportStatus.Open ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(r => new AdminReportSummaryDto(
                r.Id, r.PetId, r.Pet.Name, r.Reason, r.Status, r.Reporter.DisplayName, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminReportSummaryDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<AdminReportDto> GetReportAsync(int id, CancellationToken cancellationToken)
    {
        var report = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Pet).ThenInclude(p => p.Owner)
            .Include(r => r.Reporter)
            .Include(r => r.ReviewedBy)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Report", id);

        var others = await _db.Reports.CountAsync(
            r => r.PetId == report.PetId && r.Id != id && r.Status == ReportStatus.Open,
            cancellationToken);

        return new AdminReportDto(
            report.Id,
            report.PetId,
            report.Pet.Name,
            report.Pet.Status,
            report.Pet.IsSuspended,
            report.Pet.Owner.DisplayName,
            report.Reporter.DisplayName,
            report.Reason,
            report.Details,
            report.Status,
            report.CreatedAtUtc,
            report.ReviewedBy?.DisplayName,
            report.ReviewedAtUtc,
            report.ResolutionNote,
            others);
    }

    public async Task<AdminReportDto> ReviewReportAsync(
        int id, ReviewReportRequest request, CurrentUser admin, CancellationToken cancellationToken)
    {
        var decision = request.Decision!.Value;
        var action = request.Action;

        if (decision == ReportStatus.Open)
        {
            throw Invalid("Decision", "Decision must be Resolved or Dismissed.");
        }

        if (action == ModerationAction.Restore)
        {
            throw Invalid("Action", "Restore is not valid when reviewing a report.");
        }

        if (decision == ReportStatus.Dismissed && action != ModerationAction.None)
        {
            throw Invalid("Action", "A dismissed report cannot include a moderation action.");
        }

        var report = await _db.Reports
            .Include(r => r.Pet)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Report", id);

        var note = request.Note.Trim();
        var actionTaken = action != ModerationAction.None;

        report.Review(decision, admin.Id, note);
        NotifyReporter(report.ReporterId, report.PetId, report.Pet.Name, actionTaken);

        if (actionTaken)
        {
            await ApplyActionAsync(report.Pet, action, note, admin.Id, cancellationToken);

            var others = await _db.Reports
                .Where(r => r.PetId == report.PetId && r.Id != report.Id && r.Status == ReportStatus.Open)
                .ToListAsync(cancellationToken);

            foreach (var other in others)
            {
                other.Review(ReportStatus.Resolved, admin.Id, $"Resolved together with report #{report.Id}.");
                NotifyReporter(other.ReporterId, other.PetId, report.Pet.Name, true);
            }
        }

        await SaveAsync(cancellationToken);

        _logger.LogInformation(
            "AUDIT: admin {AdminId} reviewed report {ReportId} as {Decision} with action {Action}",
            admin.Id, id, decision, action);

        return await GetReportAsync(id, cancellationToken);
    }

    private async Task ApplyActionAsync(
        Pet pet, ModerationAction action, string note, Guid adminId, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case ModerationAction.Suspend:
                pet.Suspend(note);
                _notifications.Publish(
                    pet.OwnerId,
                    NotificationType.ListingModerated,
                    "Listing suspended",
                    $"Your listing {pet.Name} was suspended by moderation. Reason: {note}",
                    petId: pet.Id);
                break;

            case ModerationAction.Restore:
                pet.RestoreFromSuspension();
                _notifications.Publish(
                    pet.OwnerId,
                    NotificationType.ListingModerated,
                    "Listing restored",
                    $"Your listing {pet.Name} was restored. You can set it to Available again.",
                    petId: pet.Id);
                break;

            case ModerationAction.Archive:
                pet.ArchiveByModeration(note);
                _notifications.Publish(
                    pet.OwnerId,
                    NotificationType.ListingModerated,
                    "Listing removed",
                    $"Your listing {pet.Name} was removed by moderation. Reason: {note}",
                    petId: pet.Id);
                await CloseOpenApplicationsAsync(pet, adminId, cancellationToken);
                break;
        }
    }

    private async Task CloseOpenApplicationsAsync(Pet pet, Guid adminId, CancellationToken cancellationToken)
    {
        var open = await _db.Applications
            .Where(a => a.PetId == pet.Id && a.ActiveSlot != null && a.CompletedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var application in open)
        {
            application.ChangeStatus(
                ApplicationStatus.Rejected,
                ApplicationActor.PetOwner,
                adminId,
                "Closed automatically: this listing was removed by moderation.");

            var kind = application.Type == ApplicationType.Adoption ? "adoption" : "foster";

            _notifications.Publish(
                application.ApplicantId,
                NotificationType.ApplicationStatusChanged,
                "Application status changed",
                $"Your {kind} application for {pet.Name} is now {application.Status}.",
                application);
        }
    }

    private void NotifyReporter(Guid reporterId, int petId, string petName, bool actionTaken)
    {
        var outcome = actionTaken ? "action was taken" : "no action was needed";

        _notifications.Publish(
            reporterId,
            NotificationType.ReportReviewed,
            "Report reviewed",
            $"Your report about {petName} was reviewed: {outcome}. Thank you for helping keep PawConnect safe.",
            petId: petId);
    }

    private IQueryable<AdminPetDto> ProjectPets(IQueryable<Pet> query) =>
        query.Select(p => new AdminPetDto(
            p.Id,
            p.Name,
            p.Species,
            p.Status,
            p.IsSuspended,
            p.ModerationNote,
            p.Owner.DisplayName,
            _db.Reports.Count(r => r.PetId == p.Id && r.Status == ReportStatus.Open),
            p.CreatedAtUtc,
            p.UpdatedAtUtc));

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This item was changed by someone else. Reload it and try again.");
        }
    }

    private static AppValidationException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = new[] { message } });
}