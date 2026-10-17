using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PawConnect.Api.Application.DTOs.Applications;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Application.Mapping;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class ApplicationService : IApplicationService
{
    private const string DuplicateMessage = "You already have an active application for this pet.";

    private readonly AppDbContext _db;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(AppDbContext db, ILogger<ApplicationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApplicationDto> SubmitAsync(
        CreateApplicationRequest request, CurrentUser applicant, CancellationToken cancellationToken)
    {
        var type = request.Type!.Value;

        ValidateFosterFields(request, type);

        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PetId, cancellationToken);

        if (pet is null || pet.Status is ListingStatus.Draft or ListingStatus.Archived)
        {
            throw new NotFoundException(nameof(Pet), request.PetId);
        }

        if (pet.Status != ListingStatus.Available)
        {
            throw new ConflictException("This pet is not currently accepting applications.");
        }

        if (pet.OwnerId == applicant.Id)
        {
            throw new ConflictException("You cannot apply for your own listing.");
        }

        var petAcceptsType = type == ApplicationType.Adoption
            ? pet.AvailabilityType is AvailabilityType.Adoption or AvailabilityType.Both
            : pet.AvailabilityType is AvailabilityType.Foster or AvailabilityType.Both;

        if (!petAcceptsType)
        {
            throw new ConflictException(type == ApplicationType.Adoption
                ? "This pet is not available for adoption."
                : "This pet is not available for foster care.");
        }

        var alreadyActive = await _db.Applications.AnyAsync(
            a => a.PetId == pet.Id && a.ApplicantId == applicant.Id && a.ActiveSlot != null,
            cancellationToken);

        if (alreadyActive)
        {
            throw new ConflictException(DuplicateMessage);
        }

        var now = DateTime.UtcNow;

        var application = new AdoptionApplication
        {
            PetId = pet.Id,
            ApplicantId = applicant.Id,
            Type = type,
            LivingArrangement = request.LivingArrangement!.Value,
            HouseholdInfo = request.HouseholdInfo.Trim(),
            OtherPetsInfo = Clean(request.OtherPetsInfo),
            PreviousExperience = Clean(request.PreviousExperience),
            CarePlan = request.CarePlan.Trim(),
            ReasonForApplying = request.ReasonForApplying.Trim(),
            FosterStartDate = type == ApplicationType.Foster ? request.FosterStartDate : null,
            FosterDurationWeeks = type == ApplicationType.Foster ? request.FosterDurationWeeks : null,
            AdditionalNotes = Clean(request.AdditionalNotes),
            Status = ApplicationStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        application.RefreshActiveSlot();

        application.History.Add(new ApplicationStatusHistory
        {
            FromStatus = null,
            ToStatus = ApplicationStatus.Pending,
            ChangedByUserId = applicant.Id,
            ChangedAtUtc = now,
            Note = "Application submitted."
        });

        _db.Applications.Add(application);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new ConflictException(DuplicateMessage);
        }

        _logger.LogInformation(
            "Application {ApplicationId} ({Type}) submitted by {UserId} for pet {PetId}",
            application.Id, type, applicant.Id, pet.Id);

        return await GetByIdAsync(application.Id, applicant, cancellationToken);
    }

    public async Task<PagedResult<ApplicationSummaryDto>> GetMineAsync(
        CurrentUser applicant, MyApplicationsQuery parameters, CancellationToken cancellationToken)
    {
        var applicantId = applicant.Id;

        IQueryable<AdoptionApplication> query = _db.Applications
            .AsNoTracking()
            .Where(a => a.ApplicantId == applicantId);

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
            .Select(a => new ApplicationSummaryDto(
                a.Id, a.PetId, a.Pet.Name, a.Type, a.Status,
                a.FosterStartDate, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationSummaryDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<ApplicationDto> GetByIdAsync(int id, CurrentUser viewer, CancellationToken cancellationToken)
    {
        var application = await _db.Applications
            .AsNoTracking()
            .Include(a => a.Pet)
            .Include(a => a.Applicant)
            .Include(a => a.History).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Application", id);

        var isApplicant = application.ApplicantId == viewer.Id;
        var isPetOwner = application.Pet.OwnerId == viewer.Id;

        if (!isApplicant && !isPetOwner)
        {
            throw new NotFoundException("Application", id);
        }

        return application.ToDto();
    }

    private static void ValidateFosterFields(CreateApplicationRequest request, ApplicationType type)
    {
        if (type != ApplicationType.Foster)
        {
            return;
        }

        var errors = new Dictionary<string, string[]>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (request.FosterStartDate is null)
        {
            errors["FosterStartDate"] = new[] { "A start date is required for foster applications." };
        }
        else if (request.FosterStartDate < today)
        {
            errors["FosterStartDate"] = new[] { "The start date cannot be in the past." };
        }

        if (request.FosterDurationWeeks is null)
        {
            errors["FosterDurationWeeks"] = new[] { "A duration in weeks is required for foster applications." };
        }

        if (errors.Count > 0)
        {
            throw new AppValidationException(errors);
        }
    }

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}