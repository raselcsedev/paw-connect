namespace PawConnect.Api.Application.DTOs.Photos;

public record PetPhotoDto(
    int Id,
    int PetId,
    string Url,
    bool IsPrimary,
    string ContentType,
    long SizeInBytes,
    DateTime CreatedAtUtc);

public record PhotoFile(Stream Content, string ContentType);