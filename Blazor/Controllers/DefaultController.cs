using Microsoft.AspNetCore.Mvc;

namespace Blazor.Controllers;

[Route("api")]
[ApiController]
public class DefaultController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("pong");
    }

    [HttpGet("version")]
    public IActionResult Version()
    {
        return Ok(new { Version = "1.0.0" });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { Status = "Running" });
    }
}