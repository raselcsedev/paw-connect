using PawConnect.Api.Application.DTOs.Pets;

namespace PawConnect.Api.Application.Interfaces;

public interface IPetService
{
    Task<PetDto> CreateAsync(CreatePetRequest request, CancellationToken cancellationToken);
    Task<PetDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<PetSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken);
    Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CancellationToken cancellationToken);
    Task<PetDto> ChangeAvailabilityAsync(int id, ChangeAvailabilityRequest request, CancellationToken cancellationToken);
    Task ArchiveAsync(int id, CancellationToken cancellationToken);
}