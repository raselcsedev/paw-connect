using PawConnect.Api.Domain.Enums;

namespace PawConnect.Api.Application.DTOs.Pets;

public record PetSummaryDto(
    int Id,
    string Name,
    Species Species,
    string? Breed,
    int AgeInMonths,
    PetSize Size,
    string City,
    AvailabilityType AvailabilityType);

public record PetDto(
    int Id,
    string Name,
    Species Species,
    string? Breed,
    int AgeInMonths,
    PetSize Size,
    PetSex Sex,
    string? Description,
    string? HealthStatus,
    bool IsVaccinated,
    bool IsSpayedNeutered,
    string? SpecialCareRequirements,
    AvailabilityType AvailabilityType,
    string? Requirements,
    string City,
    string? Area,
    ListingStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);