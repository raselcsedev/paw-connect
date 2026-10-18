using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Application.DTOs.Photos;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Application.Services;

public class PetPhotoService : IPetPhotoService
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxPhotosPerPet = 8;

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ILogger<PetPhotoService> _logger;

    public PetPhotoService(AppDbContext db, IFileStorage storage, ILogger<PetPhotoService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PetPhotoDto>> ListAsync(
        int petId, CurrentUser? viewer, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(petId, viewer, cancellationToken);

        var photos = await _db.PetPhotos
            .AsNoTracking()
            .Where(p => p.PetId == petId)
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        return photos.Select(ToDto).ToList();
    }

    public async Task<PhotoFile> GetFileAsync(
        int petId, int photoId, CurrentUser? viewer, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(petId, viewer, cancellationToken);

        var photo = await _db.PetPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.PetId == petId, cancellationToken)
            ?? throw new NotFoundException("Photo", photoId);

        var stream = await _storage.OpenReadAsync(photo.StoredFileName, cancellationToken);

        if (stream is null)
        {
            _logger.LogError("Photo {PhotoId} exists in the database but its file is missing", photoId);
            throw new NotFoundException("Photo", photoId);
        }

        return new PhotoFile(stream, photo.ContentType);
    }

    public async Task<PetPhotoDto> UploadAsync(
        int petId, Stream content, long length, CurrentUser user, CancellationToken cancellationToken)
    {
        var pet = await LoadManageablePetAsync(petId, user, cancellationToken);

        if (pet.Status is ListingStatus.Adopted or ListingStatus.Archived)
        {
            throw new ConflictException($"A pet that is {pet.Status} cannot receive new photos.");
        }

        if (length <= 0)
        {
            throw FileError("The file is empty.");
        }

        if (length > MaxFileBytes)
        {
            throw FileError($"The file is too large. The maximum size is {MaxFileBytes / (1024 * 1024)} MB.");
        }

        var existing = await _db.PetPhotos.CountAsync(p => p.PetId == petId, cancellationToken);

        if (existing >= MaxPhotosPerPet)
        {
            throw new ConflictException($"A listing can have at most {MaxPhotosPerPet} photos.");
        }

        var header = new byte[ImageSignature.HeaderLength];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var detected = ImageSignature.Detect(header.AsSpan(0, read));

        if (detected is null)
        {
            throw FileError("Only JPEG, PNG and WebP images are accepted.");
        }

        content.Seek(0, SeekOrigin.Begin);

        var storedName = $"{Guid.NewGuid():N}{detected.Extension}";
        await _storage.SaveAsync(storedName, content, cancellationToken);

        var photo = new PetPhoto
        {
            PetId = petId,
            StoredFileName = storedName,
            ContentType = detected.ContentType,
            SizeInBytes = length,
            IsPrimary = existing == 0
        };

        _db.PetPhotos.Add(photo);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(storedName, CancellationToken.None);
            throw;
        }

        _logger.LogInformation(
            "AUDIT: user {UserId} ({Role}) uploaded photo {PhotoId} to pet {PetId}",
            user.Id, user.Role, photo.Id, petId);

        return ToDto(photo);
    }

    public async Task SetPrimaryAsync(
        int petId, int photoId, CurrentUser user, CancellationToken cancellationToken)
    {
        await LoadManageablePetAsync(petId, user, cancellationToken);

        var photos = await _db.PetPhotos
            .Where(p => p.PetId == petId)
            .ToListAsync(cancellationToken);

        if (photos.All(p => p.Id != photoId))
        {
            throw new NotFoundException("Photo", photoId);
        }

        foreach (var photo in photos)
        {
            photo.IsPrimary = photo.Id == photoId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int petId, int photoId, CurrentUser user, CancellationToken cancellationToken)
    {
        await LoadManageablePetAsync(petId, user, cancellationToken);

        var photo = await _db.PetPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.PetId == petId, cancellationToken)
            ?? throw new NotFoundException("Photo", photoId);

        _db.PetPhotos.Remove(photo);

        if (photo.IsPrimary)
        {
            var next = await _db.PetPhotos
                .Where(p => p.PetId == petId && p.Id != photoId)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (next is not null)
            {
                next.IsPrimary = true;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _storage.DeleteAsync(photo.StoredFileName, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Photo file {File} could not be deleted and is now orphaned", photo.StoredFileName);
        }

        _logger.LogInformation(
            "AUDIT: user {UserId} ({Role}) deleted photo {PhotoId} from pet {PetId}",
            user.Id, user.Role, photoId, petId);
    }

    private async Task<Pet> LoadManageablePetAsync(int petId, CurrentUser user, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), petId);

        if (pet.OwnerId == user.Id || user.IsAdmin)
        {
            return pet;
        }

        if (pet.Status is ListingStatus.Draft or ListingStatus.Archived)
        {
            throw new NotFoundException(nameof(Pet), petId);
        }

        throw new ForbiddenException("You do not have permission to change this listing's photos.");
    }

    private async Task EnsureVisibleAsync(int petId, CurrentUser? viewer, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), petId);

        var published = pet.Status is not (ListingStatus.Draft or ListingStatus.Archived);
        var isOwner = viewer is not null && pet.OwnerId == viewer.Id;
        var isAdmin = viewer is not null && viewer.IsAdmin;

        if (!published && !isOwner && !isAdmin)
        {
            throw new NotFoundException(nameof(Pet), petId);
        }
    }

    private static PetPhotoDto ToDto(PetPhoto p) => new(
        p.Id, p.PetId, $"/api/pets/{p.PetId}/photos/{p.Id}", p.IsPrimary, p.ContentType, p.SizeInBytes, p.CreatedAtUtc);

    private static AppValidationException FileError(string message) =>
        new(new Dictionary<string, string[]> { ["File"] = new[] { message } });
}