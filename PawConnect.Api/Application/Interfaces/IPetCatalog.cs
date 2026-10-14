using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Interfaces;

public interface IPetCatalog
{
    Task<IReadOnlyList<Pet>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Pet>> GetAvailablePetsAsync(CancellationToken cancellationToken);
    Task<Pet?> GetByIdAsync(int id, CancellationToken cancellationToken);
}