using Holonix.Server.Application.Interfaces;
using Holonix.Server.Contracts.Business;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/business")]
public class BusinessController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public BusinessController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tokenService = tokenService;
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

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyBusinesses(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var memberships = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.UserId == userId && businessUser.IsActive && businessUser.EndDate == null)
            .GroupJoin(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRoles) => new { businessUser.BusinessId, businessUserRoles })
            .SelectMany(
                joined => joined.businessUserRoles.DefaultIfEmpty(),
                (joined, businessUserRole) => new { joined.BusinessId, BusinessRoleId = businessUserRole != null ? businessUserRole.BusinessRoleId : (long?)null })
            .GroupJoin(
                _dbContext.BusinessRoles.AsNoTracking(),
                joined => joined.BusinessRoleId,
                businessRole => (long?)businessRole.BusinessRoleId,
                (joined, businessRoles) => new { joined.BusinessId, businessRoles })
            .SelectMany(
                joined => joined.businessRoles.DefaultIfEmpty(),
                (joined, businessRole) => new
                {
                    joined.BusinessId,
                    RoleName = businessRole != null ? businessRole.Name : null,
                })
            .GroupBy(item => item.BusinessId)
            .Select(group => new
            {
                BusinessId = group.Key,
                RoleName = group
                    .Where(item => item.RoleName != null)
                    .Select(item => item.RoleName!)
                    .OrderBy(name => name)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return Ok(Array.Empty<UserBusinessSummaryResponse>());
        }

        var businessIds = memberships
            .Select(membership => membership.BusinessId)
            .ToList();

        var roleByBusinessId = memberships.ToDictionary(
            membership => membership.BusinessId,
            membership => membership.RoleName);

        var businesses = await _dbContext.Businesses
            .AsNoTracking()
            .Where(business => businessIds.Contains(business.BusinessId))
            .Include(business => business.Details)
                .ThenInclude(details => details!.Country)
            .OrderBy(business => business.Name)
            .ToListAsync(cancellationToken);

        var serviceCounts = await _dbContext.BusinessServices
            .AsNoTracking()
            .Where(service => businessIds.Contains(service.BusinessId))
            .GroupBy(service => service.BusinessId)
            .Select(group => new { BusinessId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BusinessId, item => item.Count, cancellationToken);

        var jobCounts = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => businessIds.Contains(job.BusinessId))
            .GroupBy(job => job.BusinessId)
            .Select(group => new { BusinessId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BusinessId, item => item.Count, cancellationToken);

        var response = businesses
            .Select(business => new UserBusinessSummaryResponse(
                business.BusinessId,
                business.Name,
                business.Details?.Description,
                business.Details?.City,
                business.Details?.State,
                business.Details?.Country?.Name,
                business.Details?.BusinessIconBase64,
                business.Details?.BusinessJobPercentage ?? 0,
                business.IsProductBased,
                serviceCounts.TryGetValue(business.BusinessId, out var count) ? count : 0,
                roleByBusinessId.TryGetValue(business.BusinessId, out var roleName) ? roleName : null,
                jobCounts.TryGetValue(business.BusinessId, out var totalJobs) ? totalJobs : 0))
            .ToList();

        return Ok(response);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateBusiness(CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var validationErrors = await ValidateCreateRequestAsync(request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { errors = validationErrors });
        }

        var ownerRole = await _dbContext.BusinessRoles
            .SingleOrDefaultAsync(role => role.Name == "Owner", cancellationToken);

        if (ownerRole is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { errors = new[] { "Business owner role is unavailable." } });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var business = new Business
        {
            Name = request.Name.Trim(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsProductBased = request.IsProductBased,
            IsRecurring = false,
            Details = new BusinessDetails
            {
                Description = NormalizeOptional(request.Description),
                Address1 = request.Address1.Trim(),
                Address2 = NormalizeOptional(request.Address2),
                City = request.City.Trim(),
                State = request.State.Trim(),
                CountryId = request.CountryId,
                ZipCode = request.ZipCode.Trim(),
                BusinessIconBase64 = NormalizeBase64Image(request.BusinessIconBase64),
                BusinessJobPercentage = request.BusinessJobPercentage,
            },
        };

        _dbContext.Businesses.Add(business);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.BusinessServices.AddRange(
            request.ServiceIds
                .Distinct()
                .Select(serviceId => new BusinessService
                {
                    BusinessId = business.BusinessId,
                    ServiceId = serviceId,
                }));

        var businessUser = new BusinessUser
        {
            BusinessId = business.BusinessId,
            UserId = userId,
            IsActive = true,
            StartDate = DateTime.UtcNow,
        };

        _dbContext.BusinessUsers.Add(businessUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.BusinessUserRoles.Add(new BusinessUserRole
        {
            BusinessUserId = businessUser.BusinessUserId,
            BusinessRoleId = ownerRole.BusinessRoleId,
            StartDate = DateTime.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        var token = await _tokenService.CreateTokenAsync(user, cancellationToken);

        return Ok(new CreateBusinessResponse(
            business.BusinessId,
            business.Name,
            user.Id,
            displayName,
            token,
            user.ProfileImageBase64));
    }

    private async Task<List<string>> ValidateCreateRequestAsync(CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Business name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Address1) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) ||
            string.IsNullOrWhiteSpace(request.ZipCode))
        {
            errors.Add("Business address is required.");
        }

        if (request.BusinessJobPercentage < 0 || request.BusinessJobPercentage > 100)
        {
            errors.Add("Business earnings percentage must be between 0 and 100.");
        }

        var distinctServiceIds = request.ServiceIds
            .Where(serviceId => serviceId > 0)
            .Distinct()
            .ToArray();

        if (distinctServiceIds.Length == 0)
        {
            errors.Add("At least one business service is required.");
        }

        var countryExists = await _dbContext.Countries
            .AsNoTracking()
            .AnyAsync(country => country.CountryId == request.CountryId, cancellationToken);

        if (!countryExists)
        {
            errors.Add("Selected country was not found.");
        }

        if (distinctServiceIds.Length > 0)
        {
            var matchingServiceCount = await _dbContext.Services
                .AsNoTracking()
                .CountAsync(service => distinctServiceIds.Contains(service.ServiceId) && service.InactiveDate == null, cancellationToken);

            if (matchingServiceCount != distinctServiceIds.Length)
            {
                errors.Add("One or more selected services were not found.");
            }
        }

        return errors;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeBase64Image(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var marker = "base64,";
        var markerIndex = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return markerIndex >= 0
            ? trimmed[(markerIndex + marker.Length)..]
            : trimmed;
    }
}
