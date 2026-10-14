using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Domain.Entities;
using PawConnect.Api.Domain.Enums;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Controllers;

public record DemoPetRequest(string Name, Species Species, string? Breed, int AgeInMonths, PetSize Size);

[ApiController]
[Route("api/db-demo")]
public class DbDemoController : ControllerBase
{
    private readonly AppDbContext _db;

    public DbDemoController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("pets")]
    public async Task<IActionResult> Create(DemoPetRequest request, CancellationToken cancellationToken)
    {
        var pet = new Pet
        {
            Name = request.Name,
            Species = request.Species,
            Breed = request.Breed,
            AgeInMonths = request.AgeInMonths,
            Size = request.Size,
            Status = ListingStatus.Available
        };

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetOne), new { id = pet.Id }, pet);
    }

    [HttpGet("pets")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var pets = await _db.Pets
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(pets);
    }

    [HttpGet("pets/{id:int}")]
    public async Task<IActionResult> GetOne(int id, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Pet), id);

        return Ok(pet);
    }

    [HttpPut("pets/{id:int}")]
    public async Task<IActionResult> Update(int id, DemoPetRequest request, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        pet.Name = request.Name;
        pet.Species = request.Species;
        pet.Breed = request.Breed;
        pet.AgeInMonths = request.AgeInMonths;
        pet.Size = request.Size;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(pet);
    }

    [HttpDelete("pets/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("pets/{id:int}/tracking-demo")]
    public async Task<IActionResult> TrackingDemo(int id, CancellationToken cancellationToken)
    {
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                  ?? throw new NotFoundException(nameof(Pet), id);

        var afterLoad = _db.Entry(pet).State.ToString();

        pet.AgeInMonths += 1;
        _db.ChangeTracker.DetectChanges(); // EF normally does this for you automatically
        var afterChange = _db.Entry(pet).State.ToString();

        await _db.SaveChangesAsync(cancellationToken);
        var afterSave = _db.Entry(pet).State.ToString();

        return Ok(new { afterLoad, afterChange, afterSave, pet.AgeInMonths });
    }
}