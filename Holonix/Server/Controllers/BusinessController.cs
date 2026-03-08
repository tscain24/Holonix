using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/business")]
public class BusinessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public BusinessController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices(CancellationToken cancellationToken)
    {
        var services = await _dbContext.Services
            .AsNoTracking()
            .Where(service => service.InactiveDate == null)
            .OrderBy(service => service.Name)
            .Select(service => new
            {
                service.ServiceId,
                service.Name,
            })
            .ToListAsync(cancellationToken);

        return Ok(services);
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
    {
        var countries = await _dbContext.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .Select(country => new
            {
                country.CountryId,
                country.Name,
                country.TwoLetterIsoCode,
            })
            .ToListAsync(cancellationToken);

        return Ok(countries);
    }
}
