using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Application.Mapping;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class PetService : IPetService
{
    private readonly AppDbContext _db;
    private readonly ILogger<PetService> _logger;

    public PetService(AppDbContext db, ILogger<PetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PetDto> CreateAsync(
        CreatePetRequest request, CurrentUser owner, CancellationToken cancellationToken)
    {
        var pet = new Pet { OwnerId = owner.Id, Status = ListingStatus.Draft };
        pet.ApplyRequest(request);

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} created by {UserId}", pet.Id, owner.Id);
        return pet.ToDto();
    }

    public async Task<PetDto> GetByIdAsync(int id, CurrentUser? viewer, CancellationToken cancellationToken)
    {
        IQueryable<Pet> query = _db.Pets.AsNoTracking();

        if (viewer is null || !viewer.IsAdmin)
        {
            Guid? viewerId = viewer?.Id;

            query = query.Where(p =>
                (p.Status != ListingStatus.Draft && p.Status != ListingStatus.Archived)
                || p.OwnerId == viewerId);
        }

        var pet = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        return pet.ToDto();
    }

    public async Task<PagedResult<PetSummaryDto>> SearchAsync(
        PetQueryParameters parameters, CancellationToken cancellationToken)
    {
        IQueryable<Pet> query = _db.Pets
            .AsNoTracking()
            .Where(p => p.Status == ListingStatus.Available);

        if (parameters.Species.HasValue)
        {
            var species = parameters.Species.Value;
            query = query.Where(p => p.Species == species);
        }

        if (parameters.Size.HasValue)
        {
            var size = parameters.Size.Value;
            query = query.Where(p => p.Size == size);
        }

        if (parameters.MinAgeMonths.HasValue)
        {
            var min = parameters.MinAgeMonths.Value;
            query = query.Where(p => p.AgeInMonths >= min);
        }

        if (parameters.MaxAgeMonths.HasValue)
        {
            var max = parameters.MaxAgeMonths.Value;
            query = query.Where(p => p.AgeInMonths <= max);
        }

        if (parameters.AvailabilityType.HasValue)
        {
            var type = parameters.AvailabilityType.Value;
            query = query.Where(p => p.AvailabilityType == type || p.AvailabilityType == AvailabilityType.Both);
        }

        if (!string.IsNullOrWhiteSpace(parameters.City))
        {
            var city = parameters.City.Trim();
            query = query.Where(p => p.City == city);
        }

        if (parameters.Vaccinated.HasValue)
        {
            var vaccinated = parameters.Vaccinated.Value;
            query = query.Where(p => p.IsVaccinated == vaccinated);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Q))
        {
            var term = parameters.Q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Breed != null && p.Breed.Contains(term)) ||
                (p.Description != null && p.Description.Contains(term)));
        }

        query = parameters.Sort switch
        {
            PetSort.Oldest => query.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id),
            PetSort.AgeAsc => query.OrderBy(p => p.AgeInMonths).ThenBy(p => p.Id),
            PetSort.AgeDesc => query.OrderByDescending(p => p.AgeInMonths).ThenBy(p => p.Id),
            PetSort.NameAsc => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(p => new PetSummaryDto(
                p.Id, p.Name, p.Species, p.Breed, p.AgeInMonths, p.Size, p.City, p.AvailabilityType))
            .ToListAsync(cancellationToken);

        return new PagedResult<PetSummaryDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<PagedResult<PetDto>> GetMineAsync(
        CurrentUser owner, MyPetsQuery parameters, CancellationToken cancellationToken)
    {
        var ownerId = owner.Id;

        IQueryable<Pet> query = _db.Pets
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId);

        if (parameters.Status.HasValue)
        {
            var status = parameters.Status.Value;
            query = query.Where(p => p.Status == status);
        }

        query = query.OrderByDescending(p => p.UpdatedAtUtc).ThenByDescending(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var pets = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PetDto>(
            pets.Select(p => p.ToDto()).ToList(), parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<PetDto> UpdateAsync(
        int id, UpdatePetRequest request, CurrentUser user, CancellationToken cancellationToken)
    {
        var pet = await LoadForManagementAsync(id, user, allowAdmin: false, cancellationToken);

        pet.EnsureEditable();
        pet.ApplyRequest(request);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} updated by {UserId}", id, user.Id);
        return pet.ToDto();
    }

    public async Task<PetDto> ChangeAvailabilityAsync(
        int id, ChangeAvailabilityRequest request, CurrentUser user, CancellationToken cancellationToken)
    {
        var pet = await LoadForManagementAsync(id, user, allowAdmin: true, cancellationToken);

        var oldStatus = pet.Status;
        pet.SetAvailability(request.Status!.Value);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AUDIT: user {UserId} ({Role}) changed pet {PetId} status from {Old} to {New}",
            user.Id, user.Role, id, oldStatus, pet.Status);
        return pet.ToDto();
    }

    public async Task ArchiveAsync(int id, CurrentUser user, CancellationToken cancellationToken)
    {
        var pet = await LoadForManagementAsync(id, user, allowAdmin: true, cancellationToken);

        if (pet.Status == ListingStatus.Archived)
        {
            return;
        }

        pet.Archive();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AUDIT: user {UserId} ({Role}) archived pet {PetId}", user.Id, user.Role, id);
    }

    private async Task<Pet> LoadForManagementAsync(
        int id, CurrentUser user, bool allowAdmin, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        if (pet.OwnerId == user.Id || (allowAdmin && user.IsAdmin))
        {
            return pet;
        }

        var isHidden = pet.Status is ListingStatus.Draft or ListingStatus.Archived;

        if (isHidden)
        {
            throw new NotFoundException(nameof(Pet), id);
        }

        throw new ForbiddenException("You do not have permission to modify this listing.");
    }
}