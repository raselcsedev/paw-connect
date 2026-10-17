using PawConnect.Api.Application.DTOs.Common;
using PawConnect.Api.Application.DTOs.Pets;

namespace PawConnect.Api.Application.Interfaces;

public interface IPetService
{
    Task<PetDto> CreateAsync(CreatePetRequest request, CurrentUser owner, CancellationToken cancellationToken);
    Task<PetDto> GetByIdAsync(int id, CurrentUser? viewer, CancellationToken cancellationToken);
    Task<PagedResult<PetSummaryDto>> SearchAsync(PetQueryParameters query, CancellationToken cancellationToken);
    Task<PagedResult<PetDto>> GetMineAsync(CurrentUser owner, MyPetsQuery query, CancellationToken cancellationToken);
    Task<PetDto> UpdateAsync(int id, UpdatePetRequest request, CurrentUser user, CancellationToken cancellationToken);
    Task<PetDto> ChangeAvailabilityAsync(int id, ChangeAvailabilityRequest request, CurrentUser user, CancellationToken cancellationToken);
    Task ArchiveAsync(int id, CurrentUser user, CancellationToken cancellationToken);
}