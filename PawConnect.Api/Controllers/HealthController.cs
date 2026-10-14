using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PawConnect.Api.Infrastructure.Data;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "PawConnect.Api",
            timeUtc = DateTime.UtcNow
        });
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new { version = "0.1.0" });
    }

    [HttpGet("db")]
    public async Task<IActionResult> GetDatabase(CancellationToken cancellationToken)
    {
        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { database = "unreachable" });
        }

        var petCount = await _db.Pets.CountAsync(cancellationToken);
        return Ok(new { database = "connected", petCount });
    }
}