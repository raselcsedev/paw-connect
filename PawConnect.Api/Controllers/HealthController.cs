using Microsoft.AspNetCore.Mvc;

namespace PawConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
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
}