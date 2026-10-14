using Microsoft.AspNetCore.Mvc;
using PawConnect.Api.Application.Interfaces;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/scratch")]
public class ScratchController : ControllerBase
{
    private readonly IPetCatalog _catalog;
    private readonly ILogger<ScratchController> _logger;

    public ScratchController(IPetCatalog catalog, ILogger<ScratchController> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    [HttpGet("pet")]
    public IActionResult GetSamplePet()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Milo",
            Species = Species.Dog,
            Breed = "Labrador Mix",
            AgeInMonths = 18,
            Size = PetSize.Medium,
            Status = ListingStatus.Available
        };

        return Ok(new { pet, isBrowsable = pet.IsBrowsable() });
    }

    [HttpGet("pets")]
    public async Task<IActionResult> GetPets(CancellationToken cancellationToken)
    {
        var pets = await _catalog.GetAvailablePetsAsync(cancellationToken);
        return Ok(pets);
    }

    [HttpGet("pets/{id:int}")]
    public async Task<IActionResult> GetPet(int id, CancellationToken cancellationToken)
    {
        var pet = await _catalog.GetByIdAsync(id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        return Ok(pet);
    }

    [HttpGet("slow")]
    public async Task<IActionResult> GetSlow(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Slow request started");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return Ok(new { message = "Finished after 5 seconds" });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Slow request was cancelled by the client");
            throw;
        }
    }
    [HttpGet("search")]
    public async Task<IActionResult> Search(
    [FromQuery] Species? species,
    [FromQuery] PetSize? size,
    [FromQuery] int? minAgeMonths,
    [FromQuery] int? maxAgeMonths,
    [FromQuery] string? q,
    [FromQuery] string sort = "newest",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 3,
    CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var all = await _catalog.GetAllAsync(cancellationToken);

        IEnumerable<Pet> query = all.Where(p => p.IsBrowsable());

        if (species.HasValue)
        {
            query = query.Where(p => p.Species == species.Value);
        }

        if (size.HasValue)
        {
            query = query.Where(p => p.Size == size.Value);
        }

        if (minAgeMonths.HasValue)
        {
            query = query.Where(p => p.AgeInMonths >= minAgeMonths.Value);
        }

        if (maxAgeMonths.HasValue)
        {
            query = query.Where(p => p.AgeInMonths <= maxAgeMonths.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (p.Breed?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = sort switch
        {
            "age" => query.OrderBy(p => p.AgeInMonths),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };

        var totalCount = query.Count();

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new { p.Id, p.Name, p.Species, p.Breed, p.AgeInMonths })
            .ToList();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items
        });
    }

    [HttpGet("boom")]
    public IActionResult Boom() =>
        throw new InvalidOperationException("Internal detail: connection string is Server=secret;Password=hunter2");

    [HttpGet("conflict")]
    public IActionResult ConflictDemo() =>
        throw new ConflictException("You already have an active application for this pet.");
}