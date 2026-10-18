using PawConnect.Api.Application.DTOs.Photos;

namespace PawConnect.Api.Application.Interfaces;

public interface IPetPhotoService
{
    Task<IReadOnlyList<PetPhotoDto>> ListAsync(int petId, CurrentUser? viewer, CancellationToken cancellationToken);
    Task<PhotoFile> GetFileAsync(int petId, int photoId, CurrentUser? viewer, CancellationToken cancellationToken);
    Task<PetPhotoDto> UploadAsync(int petId, Stream content, long length, CurrentUser user, CancellationToken cancellationToken);
    Task SetPrimaryAsync(int petId, int photoId, CurrentUser user, CancellationToken cancellationToken);
    Task DeleteAsync(int petId, int photoId, CurrentUser user, CancellationToken cancellationToken);
}