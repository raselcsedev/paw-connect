using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class ApplicationReviewService : IApplicationReviewService
{
    private readonly AppDbContext _db;
    private readonly IApplicationService _applications;
    private readonly ILogger<ApplicationReviewService> _logger;

    public ApplicationReviewService(
        AppDbContext db,
        IApplicationService applications,
        ILogger<ApplicationReviewService> logger)
    {
        _db = db;
        _applications = applications;
        _logger = logger;
    }

    public async Task<PagedResult<IncomingApplicationDto>> GetIncomingAsync(
        CurrentUser owner, IncomingApplicationsQuery parameters, CancellationToken cancellationToken)
    {
        var ownerId = owner.Id;

        if (parameters.PetId.HasValue)
        {
            var petId = parameters.PetId.Value;
            var ownsPet = await _db.Pets.AnyAsync(p => p.Id == petId && p.OwnerId == ownerId, cancellationToken);

            if (!ownsPet)
            {
                throw new NotFoundException(nameof(Pet), petId);
            }
        }

        IQueryable<AdoptionApplication> query = _db.Applications
            .AsNoTracking()
            .Where(a => a.Pet.OwnerId == ownerId);

        if (parameters.PetId.HasValue)
        {
            var petId = parameters.PetId.Value;
            query = query.Where(a => a.PetId == petId);
        }

        if (parameters.Status.HasValue)
        {
            var status = parameters.Status.Value;
            query = query.Where(a => a.Status == status);
        }

        if (parameters.Type.HasValue)
        {
            var type = parameters.Type.Value;
            query = query.Where(a => a.Type == type);
        }

        query = query.OrderByDescending(a => a.CreatedAtUtc).ThenByDescending(a => a.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(a => new IncomingApplicationDto(
                a.Id, a.PetId, a.Pet.Name, a.Applicant.DisplayName, a.Type, a.Status,
                a.FosterStartDate, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<IncomingApplicationDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<ApplicationDto> ChangeStatusAsync(
        int id, UpdateApplicationStatusRequest request, CurrentUser owner, CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .Include(a => a.Pet)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Application", id);

        if (application.Pet.OwnerId != owner.Id)
        {
            throw new NotFoundException("Application", id);
        }

        if (application.Version != request.Version)
        {
            throw new ConflictException(
                "This application was changed by someone else. Reload it and try again.");
        }

        var newStatus = request.Status!.Value;

        if (newStatus == ApplicationStatus.Rejected && string.IsNullOrWhiteSpace(request.Note))
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Note"] = new[] { "A note explaining the decision is required when rejecting an application." }
            });
        }

        var movesForward = newStatus is ApplicationStatus.UnderReview or ApplicationStatus.Approved;
        var listingClosed = application.Pet.Status is ListingStatus.Adopted or ListingStatus.Archived;

        if (movesForward && listingClosed)
        {
            throw new ConflictException("This listing is closed, so the application cannot move forward.");
        }

        var oldStatus = application.Status;
        application.ChangeStatus(newStatus, ApplicationActor.PetOwner, owner.Id, request.Note);

        await SaveAsync(cancellationToken);

        _logger.LogInformation(
            "AUDIT: user {UserId} moved application {ApplicationId} from {Old} to {New}",
            owner.Id, id, oldStatus, newStatus);

        return await _applications.GetByIdAsync(id, owner, cancellationToken);
    }

    public async Task<ApplicationDto> WithdrawAsync(
        int id, CurrentUser applicant, CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Application", id);

        if (application.ApplicantId != applicant.Id)
        {
            throw new NotFoundException("Application", id);
        }

        application.ChangeStatus(
            ApplicationStatus.Withdrawn, ApplicationActor.Applicant, applicant.Id, "Withdrawn by the applicant.");

        await SaveAsync(cancellationToken);

        _logger.LogInformation(
            "AUDIT: applicant {UserId} withdrew application {ApplicationId}", applicant.Id, id);

        return await _applications.GetByIdAsync(id, applicant, cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This application was changed by someone else. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            throw new ConflictException("Another adoption application for this pet is already approved.");
        }
    }
}