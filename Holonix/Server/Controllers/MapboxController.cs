using Holonix.Server.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/mapbox")]
public sealed class MapboxController : ControllerBase
{
    private readonly MapboxOptions _options;

    public MapboxController(IOptions<MapboxOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet("public-token")]
    public IActionResult GetPublicToken()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            return BadRequest(new { errors = new[] { "Mapbox access token is missing." } });
        }

        return Ok(new
        {
            accessToken = _options.AccessToken.Trim(),
        });
    }
}

