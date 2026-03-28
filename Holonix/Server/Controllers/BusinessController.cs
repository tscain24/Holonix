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
            .Where(business => businessIds.Contains(business.BusinessId) && business.InactiveDate == null)
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
    [HttpGet("{businessId:int}")]
    public async Task<IActionResult> GetBusinessWorkspace(int businessId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId &&
                businessUser.UserId == userId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .GroupJoin(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRoles) => new { businessUser.BusinessUserId, businessUserRoles })
            .SelectMany(
                joined => joined.businessUserRoles.DefaultIfEmpty(),
                (joined, businessUserRole) => new
                {
                    joined.BusinessUserId,
                    BusinessRoleId = businessUserRole != null ? businessUserRole.BusinessRoleId : (long?)null,
                })
            .GroupJoin(
                _dbContext.BusinessRoles.AsNoTracking(),
                joined => joined.BusinessRoleId,
                businessRole => (long?)businessRole.BusinessRoleId,
                (joined, businessRoles) => new { joined.BusinessUserId, businessRoles })
            .SelectMany(
                joined => joined.businessRoles.DefaultIfEmpty(),
                (joined, businessRole) => new
                {
                    joined.BusinessUserId,
                    RoleName = businessRole != null ? businessRole.Name : null,
                })
            .OrderBy(item => item.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null)
        {
            return Forbid();
        }

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .Where(item => item.BusinessId == businessId && item.InactiveDate == null)
            .Include(item => item.Details)
                .ThenInclude(details => details!.Country)
            .SingleOrDefaultAsync(cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var services = await _dbContext.BusinessServices
            .AsNoTracking()
            .Where(item => item.BusinessId == businessId)
            .Join(
                _dbContext.Services.AsNoTracking(),
                businessService => businessService.ServiceId,
                service => service.ServiceId,
                (businessService, service) => new
                {
                    service.ServiceId,
                    service.Name,
                })
            .OrderBy(item => item.Name)
            .Select(item => new BusinessWorkspaceServiceResponse(item.ServiceId, item.Name))
            .ToListAsync(cancellationToken);

        var employeeRoleRows = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.BusinessId == businessId && businessUser.EndDate == null)
            .Join(
                _dbContext.Users.AsNoTracking(),
                businessUser => businessUser.UserId,
                user => user.Id,
                (businessUser, user) => new
                {
                    businessUser.BusinessUserId,
                    businessUser.IsActive,
                    businessUser.StartDate,
                    user.FirstName,
                    user.LastName,
                })
            .GroupJoin(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRoles) => new { businessUser, businessUserRoles })
            .SelectMany(
                joined => joined.businessUserRoles.DefaultIfEmpty(),
                (joined, businessUserRole) => new
                {
                    joined.businessUser.BusinessUserId,
                    joined.businessUser.IsActive,
                    joined.businessUser.StartDate,
                    joined.businessUser.FirstName,
                    joined.businessUser.LastName,
                    BusinessRoleId = businessUserRole != null ? businessUserRole.BusinessRoleId : (long?)null,
                })
            .GroupJoin(
                _dbContext.BusinessRoles.AsNoTracking(),
                joined => joined.BusinessRoleId,
                businessRole => (long?)businessRole.BusinessRoleId,
                (joined, businessRoles) => new { joined, businessRoles })
            .SelectMany(
                joined => joined.businessRoles.DefaultIfEmpty(),
                (joined, businessRole) => new
                {
                    joined.joined.BusinessUserId,
                    joined.joined.IsActive,
                    joined.joined.StartDate,
                    joined.joined.FirstName,
                    joined.joined.LastName,
                    RoleName = businessRole != null ? businessRole.Name : null,
                })
            .ToListAsync(cancellationToken);

        var assignedJobCounts = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId && job.BusinessUserId.HasValue)
            .GroupBy(job => job.BusinessUserId!.Value)
            .Select(group => new { BusinessUserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BusinessUserId, item => item.Count, cancellationToken);

        var employees = employeeRoleRows
            .GroupBy(item => item.BusinessUserId)
            .Select(group =>
            {
                var first = group
                    .OrderByDescending(item => item.IsActive)
                    .ThenBy(item => item.StartDate)
                    .First();

                var displayName = $"{first.FirstName} {first.LastName}".Trim();
                var roleName = group
                    .Where(item => !string.IsNullOrWhiteSpace(item.RoleName))
                    .Select(item => item.RoleName!)
                    .OrderBy(name => name)
                    .FirstOrDefault();

                return new BusinessWorkspaceEmployeeResponse(
                    group.Key,
                    string.IsNullOrWhiteSpace(displayName) ? "Team Member" : displayName,
                    roleName,
                    first.StartDate,
                    first.IsActive,
                    assignedJobCounts.TryGetValue(group.Key, out var assignedJobCount) ? assignedJobCount : 0);
            })
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.DisplayName)
            .ToList();

        var jobs = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId)
            .OrderByDescending(job => job.StartDateTime)
            .Select(job => new
            {
                job.JobId,
                job.Name,
                job.StartDateTime,
                job.EndDateTime,
                job.Cost,
                job.NetCost,
                job.BusinessUserId,
            })
            .Take(8)
            .ToListAsync(cancellationToken);

        var employeeNameByBusinessUserId = employees.ToDictionary(
            employee => employee.BusinessUserId,
            employee => employee.DisplayName);

        var jobResponses = jobs
            .Select(job => new BusinessWorkspaceJobResponse(
                job.JobId,
                string.IsNullOrWhiteSpace(job.Name) ? $"Job #{job.JobId}" : job.Name,
                job.StartDateTime,
                job.EndDateTime,
                job.Cost,
                job.NetCost,
                job.BusinessUserId.HasValue && employeeNameByBusinessUserId.TryGetValue(job.BusinessUserId.Value, out var employeeName)
                    ? employeeName
                    : null))
            .ToList();

        var totalRevenue = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId)
            .Select(job => (decimal?)job.NetCost)
            .SumAsync(cancellationToken) ?? 0m;

        var totalJobs = await _dbContext.Jobs
            .AsNoTracking()
            .CountAsync(job => job.BusinessId == businessId, cancellationToken);

        var activeEmployeeCount = employees.Count(employee => employee.IsActive);

        var response = new BusinessWorkspaceResponse(
            business.BusinessId,
            business.Name,
            business.Details?.Description,
            business.Details?.Address1,
            business.Details?.Address2,
            business.Details?.City,
            business.Details?.State,
            business.Details?.ZipCode,
            business.Details?.Country?.Name,
            business.Details?.BusinessIconBase64,
            business.Details?.BusinessJobPercentage ?? 0,
            business.IsProductBased,
            currentMembership.RoleName,
            services.Count,
            services,
            activeEmployeeCount,
            totalJobs,
            totalRevenue,
            employees,
            jobResponses);

        return Ok(response);
    }

    [Authorize]
    [HttpPut("{businessId:int}")]
    public async Task<IActionResult> UpdateBusinessProfile(int businessId, UpdateBusinessProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId &&
                businessUser.UserId == userId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Join(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRole) => businessUserRole.BusinessRoleId)
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                businessRoleId => businessRoleId,
                businessRole => businessRole.BusinessRoleId,
                (_, businessRole) => businessRole.Name)
            .AnyAsync(roleName => roleName == BusinessRoleNames.Owner, cancellationToken);

        if (!isOwner)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { errors = new[] { "Business name is required." } });
        }

        if (string.IsNullOrWhiteSpace(request.Address1) ||
            string.IsNullOrWhiteSpace(request.City) ||
            string.IsNullOrWhiteSpace(request.State) ||
            string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return BadRequest(new { errors = new[] { "Business address is required." } });
        }

        if (request.BusinessJobPercentage < 0 || request.BusinessJobPercentage > 100)
        {
            return BadRequest(new { errors = new[] { "Business earnings percentage must be between 0 and 100." } });
        }

        var business = await _dbContext.Businesses
            .Include(item => item.Details)
            .ThenInclude(details => details!.Country)
            .SingleOrDefaultAsync(item => item.BusinessId == businessId && item.InactiveDate == null, cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        business.Name = request.Name.Trim();
        business.Details ??= new BusinessDetails { BusinessId = business.BusinessId };
        business.Details.Description = NormalizeOptional(request.Description);
        business.Details.BusinessIconBase64 = NormalizeBase64Image(request.BusinessIconBase64);
        business.Details.Address1 = request.Address1.Trim();
        business.Details.Address2 = NormalizeOptional(request.Address2);
        business.Details.City = request.City.Trim();
        business.Details.State = request.State.Trim();
        business.Details.ZipCode = request.ZipCode.Trim();
        business.Details.BusinessJobPercentage = request.BusinessJobPercentage;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UpdateBusinessProfileResponse(
            business.BusinessId,
            business.Name,
            business.Details.Description,
            business.Details.BusinessIconBase64,
            business.Details.Address1,
            business.Details.Address2,
            business.Details.City,
            business.Details.State,
            business.Details.ZipCode,
            business.Details.Country?.Name,
            business.Details.BusinessJobPercentage));
    }

    [Authorize]
    [HttpPut("{businessId:int}/services")]
    public async Task<IActionResult> UpdateBusinessServices(int businessId, UpdateBusinessServicesRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId &&
                businessUser.UserId == userId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Join(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRole) => businessUserRole.BusinessRoleId)
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                businessRoleId => businessRoleId,
                businessRole => businessRole.BusinessRoleId,
                (_, businessRole) => businessRole.Name)
            .AnyAsync(roleName => roleName == BusinessRoleNames.Owner, cancellationToken);

        if (!isOwner)
        {
            return Forbid();
        }

        var serviceIds = request.ServiceIds
            .Where(serviceId => serviceId > 0)
            .Distinct()
            .ToArray();

        if (serviceIds.Length == 0)
        {
            return BadRequest(new { errors = new[] { "At least one service is required." } });
        }

        var businessExists = await _dbContext.Businesses
            .AsNoTracking()
            .AnyAsync(business => business.BusinessId == businessId && business.InactiveDate == null, cancellationToken);

        if (!businessExists)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var validServices = await _dbContext.Services
            .AsNoTracking()
            .Where(service => serviceIds.Contains(service.ServiceId) && service.InactiveDate == null)
            .Select(service => new
            {
                service.ServiceId,
                service.Name,
            })
            .ToListAsync(cancellationToken);

        if (validServices.Count != serviceIds.Length)
        {
            return BadRequest(new { errors = new[] { "One or more selected services were not found." } });
        }

        var existingServices = await _dbContext.BusinessServices
            .Where(item => item.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        _dbContext.BusinessServices.RemoveRange(existingServices);
        _dbContext.BusinessServices.AddRange(
            serviceIds.Select(serviceId => new BusinessService
            {
                BusinessId = businessId,
                ServiceId = serviceId,
            }));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(validServices
            .OrderBy(item => item.Name)
            .Select(item => new BusinessWorkspaceServiceResponse(item.ServiceId, item.Name))
            .ToList());
    }

    [Authorize]
    [HttpDelete("{businessId:int}")]
    public async Task<IActionResult> DeleteBusiness(int businessId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId &&
                businessUser.UserId == userId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Join(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRole) => businessUserRole.BusinessRoleId)
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                businessRoleId => businessRoleId,
                businessRole => businessRole.BusinessRoleId,
                (_, businessRole) => businessRole.Name)
            .AnyAsync(roleName => roleName == BusinessRoleNames.Owner, cancellationToken);

        if (!isOwner)
        {
            return Forbid();
        }

        var business = await _dbContext.Businesses
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.BusinessId == businessId && item.InactiveDate == null, cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var businessUserIds = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.BusinessId == businessId)
            .Select(businessUser => businessUser.BusinessUserId)
            .ToListAsync(cancellationToken);

        var endedAt = DateTime.UtcNow;
        business.InactiveDate = endedAt;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (businessUserIds.Count > 0)
        {
            await _dbContext.BusinessUserRoles
                .Where(item => businessUserIds.Contains(item.BusinessUserId) && item.EndDate == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.EndDate, endedAt), cancellationToken);

            await _dbContext.BusinessUsers
                .Where(item => item.BusinessId == businessId && (item.IsActive || item.EndDate == null))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.IsActive, false)
                    .SetProperty(item => item.EndDate, endedAt), cancellationToken);
        }

        return NoContent();
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
            .SingleOrDefaultAsync(role => role.Name == BusinessRoleNames.Owner, cancellationToken);

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
