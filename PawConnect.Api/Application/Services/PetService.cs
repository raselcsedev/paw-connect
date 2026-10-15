using Microsoft.EntityFrameworkCore;
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

    public async Task<IReadOnlyList<PetSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken)
    {
        return await _db.Pets
            .AsNoTracking()
            .Where(p => p.Status == ListingStatus.Available)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(50)
            .Select(p => new PetSummaryDto(
                p.Id, p.Name, p.Species, p.Breed, p.AgeInMonths, p.Size, p.City, p.AvailabilityType))
            .ToListAsync(cancellationToken);
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