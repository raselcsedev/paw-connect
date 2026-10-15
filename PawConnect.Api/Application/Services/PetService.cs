using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Pets;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Application.Mapping;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;
using PawConnect.Api.Application.DTOs.Common;

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

    public async Task<PetDto> CreateAsync(CreatePetRequest request, CancellationToken cancellationToken)
    {
        var pet = new Pet { Status = ListingStatus.Draft };
        pet.ApplyRequest(request);

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} created", pet.Id);
        return pet.ToDto();
    }

    public async Task<PetDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
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

    public async Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken)
    {
        var pet = await FindTrackedAsync(id, cancellationToken);

        pet.EnsureEditable();
        pet.ApplyRequest(request);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} updated", id);
        return pet.ToDto();
    }

    public async Task<PetDto> ChangeAvailabilityAsync(
        int id, ChangeAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var pet = await FindTrackedAsync(id, cancellationToken);

        var oldStatus = pet.Status;
        pet.SetAvailability(request.Status!.Value);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} status changed from {Old} to {New}", id, oldStatus, pet.Status);
        return pet.ToDto();
    }

    public async Task ArchiveAsync(int id, CancellationToken cancellationToken)
    {
        var pet = await FindTrackedAsync(id, cancellationToken);

        if (pet.Status == ListingStatus.Archived)
        {
            return;
        }

        pet.Archive();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Pet {PetId} archived", id);
    }

    private async Task<Pet> FindTrackedAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
               ?? throw new NotFoundException(nameof(Pet), id);
    }
}