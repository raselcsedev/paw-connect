using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public record PetSummaryDto(
    int Id,
    string Name,
    Species Species,
    string? Breed,
    int AgeInMonths,
    PetSize Size);

public record PetDto(
    int Id,
    string Name,
    Species Species,
    string? Breed,
    int AgeInMonths,
    PetSize Size,
    ListingStatus Status,
    DateTime CreatedAtUtc);