using PawConnect.Api.Domain.Entities;

namespace PawConnect.Api.Application.Interfaces;

public interface IPetCatalog
{
    IReadOnlyList<Pet> GetAvailablePets();
    Pet? GetById(int id);
}