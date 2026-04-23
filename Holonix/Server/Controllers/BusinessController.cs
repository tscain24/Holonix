using Holonix.Server.Application.Interfaces;
using Holonix.Server.Contracts.Business;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/business")]
public class BusinessController : ControllerBase
{
    private const int BusinessCodeLength = 10;
    private const string BusinessCodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
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
                business.BusinessCode,
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
    [HttpGet("{businessCode}")]
    public async Task<IActionResult> GetBusinessWorkspace(string businessCode, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .ThenBy(item => item.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null)
        {
            return Forbid();
        }

        var business = await _dbContext.Businesses
            .AsNoTracking()
            .Where(item => item.BusinessId == businessId.Value && item.InactiveDate == null)
            .Include(item => item.Details)
                .ThenInclude(details => details!.Country)
            .SingleOrDefaultAsync(cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var services = await _dbContext.BusinessServices
            .AsNoTracking()
            .Where(item => item.BusinessId == businessId.Value)
            .Join(
                _dbContext.Services.AsNoTracking(),
                businessService => businessService.ServiceId,
                service => service.ServiceId,
                (businessService, service) => new
                {
                    service.ServiceId,
                    service.Name,
                })
            .ToListAsync(cancellationToken);

        var subServicesByServiceId = await GetActiveSubServicesByServiceIdAsync(
            businessId.Value,
            services.Select(item => item.ServiceId).ToArray(),
            cancellationToken);

        var serviceResponses = services
            .OrderBy(item => item.Name)
            .Select(item => new BusinessWorkspaceServiceResponse(
                item.ServiceId,
                item.Name,
                subServicesByServiceId.TryGetValue(item.ServiceId, out var subServices)
                    ? subServices
                    : Array.Empty<BusinessWorkspaceSubServiceResponse>()))
            .ToList();

        var availableEmployeeRoles = await _dbContext.BusinessRoles
            .AsNoTracking()
            .Where(role =>
                currentMembership.RoleHierarchyNumber.HasValue &&
                role.HierarchyNumber <= currentMembership.RoleHierarchyNumber.Value)
            .OrderByDescending(role => role.HierarchyNumber)
            .ThenBy(role => role.Name)
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);

        var employeeRoleRows = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.BusinessId == businessId.Value && businessUser.EndDate == null)
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
                    user.Email,
                    user.PhoneNumber,
                    user.DateOfBirth,
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
                    joined.businessUser.Email,
                    joined.businessUser.PhoneNumber,
                    joined.businessUser.DateOfBirth,
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
                    joined.joined.Email,
                    joined.joined.PhoneNumber,
                    joined.joined.DateOfBirth,
                    RoleName = businessRole != null ? businessRole.Name : null,
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .ToListAsync(cancellationToken);

        var assignedJobCounts = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId.Value && job.BusinessUserId.HasValue)
            .GroupBy(job => job.BusinessUserId!.Value)
            .Select(group => new { BusinessUserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BusinessUserId, item => item.Count, cancellationToken);

        var orderedEmployees = employeeRoleRows
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

                var roleHierarchyNumber = group
                    .Where(item => item.RoleHierarchyNumber.HasValue)
                    .Select(item => item.RoleHierarchyNumber)
                    .OrderByDescending(value => value)
                    .FirstOrDefault();

                var canDeactivate = CanManageEmployeeInvites(currentMembership.RoleName)
                    && first.IsActive
                    && group.Key != currentMembership.BusinessUserId
                    && currentMembership.RoleHierarchyNumber.HasValue
                    && roleHierarchyNumber.HasValue
                    && roleHierarchyNumber.Value < currentMembership.RoleHierarchyNumber.Value;

                var canUpdateRole = CanManageEmployeeInvites(currentMembership.RoleName)
                    && first.IsActive
                    && group.Key != currentMembership.BusinessUserId
                    && currentMembership.RoleHierarchyNumber.HasValue
                    && roleHierarchyNumber.HasValue
                    && roleHierarchyNumber.Value < currentMembership.RoleHierarchyNumber.Value;

                var sortLastName = first.LastName?.Trim() ?? string.Empty;
                var sortFirstName = first.FirstName?.Trim() ?? string.Empty;

                return new
                {
                    SortLastName = sortLastName,
                    SortFirstName = sortFirstName,
                    Employee = new BusinessWorkspaceEmployeeResponse(
                        group.Key,
                        string.IsNullOrWhiteSpace(displayName) ? "Team Member" : displayName,
                        first.Email,
                        first.PhoneNumber,
                        first.DateOfBirth,
                        roleName,
                        first.StartDate,
                        first.IsActive,
                        assignedJobCounts.TryGetValue(group.Key, out var assignedJobCount) ? assignedJobCount : 0,
                        canDeactivate,
                        canUpdateRole),
                };
            })
            .OrderByDescending(item => item.Employee.IsActive)
            .ThenBy(item => string.IsNullOrWhiteSpace(item.SortLastName) ? item.Employee.DisplayName : item.SortLastName)
            .ThenBy(item => string.IsNullOrWhiteSpace(item.SortFirstName) ? item.Employee.DisplayName : item.SortFirstName)
            .Select(item => item.Employee)
            .ToList();

        var filterEmployeeRoles = employeeRoleRows
            .Where(item => !string.IsNullOrWhiteSpace(item.RoleName))
            .Select(item => item.RoleName!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(roleName => roleName.Equals(BusinessRoleNames.Owner, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(roleName => roleName.Equals(BusinessRoleNames.Administrator, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(roleName => roleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            .ThenBy(roleName => roleName)
            .ToList();

        var employees = orderedEmployees
            .Take(5)
            .ToList();

        var jobs = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId.Value)
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

        var employeeNameByBusinessUserId = orderedEmployees.ToDictionary(
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
            .Where(job => job.BusinessId == businessId.Value)
            .Select(job => (decimal?)job.NetCost)
            .SumAsync(cancellationToken) ?? 0m;

        var totalJobs = await _dbContext.Jobs
            .AsNoTracking()
            .CountAsync(job => job.BusinessId == businessId.Value, cancellationToken);

        var activeEmployeeCount = orderedEmployees.Count(employee => employee.IsActive);

        var response = new BusinessWorkspaceResponse(
            business.BusinessId,
            business.BusinessCode,
            business.Name,
            business.Details?.Description,
            business.Details?.BusinessEmail,
            business.Details?.BusinessPhoneNumber,
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
            availableEmployeeRoles,
            filterEmployeeRoles,
            serviceResponses.Count,
            serviceResponses,
            activeEmployeeCount,
            totalJobs,
            totalRevenue,
            employees,
            jobResponses);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("{businessCode}/my-availability")]
    public async Task<IActionResult> GetMyBusinessAvailability(string businessCode, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var businessUserId = await GetActiveBusinessUserIdAsync(businessId.Value, userId, cancellationToken);
        if (!businessUserId.HasValue)
        {
            return Forbid();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var availabilityRows = await _dbContext.BusinessUserAvailabilities
            .AsNoTracking()
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate <= today &&
                (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= today))
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .ToListAsync(cancellationToken);

        var dayOffRows = await _dbContext.BusinessUserTimeOffs
            .AsNoTracking()
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate.HasValue &&
                item.EffectiveEndDate.HasValue &&
                item.EffectiveEndDate.Value >= today)
            .OrderBy(item => item.EffectiveStartDate)
            .ThenBy(item => item.EffectiveEndDate)
            .ThenBy(item => item.EffectiveStartTime)
            .ToListAsync(cancellationToken);

        var effectiveStartDate = availabilityRows.Count > 0
            ? availabilityRows.Min(item => item.EffectiveStartDate)
            : today;

        return Ok(new MyBusinessAvailabilityResponse(
            businessUserId.Value,
            effectiveStartDate,
            availabilityRows
                .Select(item => new MyBusinessAvailabilityDayResponse(
                    item.DayOfWeek,
                    item.StartTime,
                    item.EndTime))
                .ToList(),
            dayOffRows
                .Select(item => new MyBusinessTimeOffResponse(
                    item.BusinessUserTimeOffId,
                    item.EffectiveStartDate!.Value,
                    item.EffectiveEndDate!.Value,
                    item.EffectiveStartTime ?? default,
                    item.EffectiveEndTime ?? default))
                .ToList()));
    }

    [Authorize]
    [HttpGet("{businessCode}/my-availability/daily")]
    public async Task<IActionResult> GetMyBusinessDailyAvailability(
        string businessCode,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            return BadRequest(new { errors = new[] { "End date must be on or after the start date." } });
        }

        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (totalDays > 366)
        {
            return BadRequest(new { errors = new[] { "Date range must be 366 days or fewer." } });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var businessUserId = await GetActiveBusinessUserIdAsync(businessId.Value, userId, cancellationToken);
        if (!businessUserId.HasValue)
        {
            return Forbid();
        }

        var availabilityRows = await _dbContext.BusinessUserAvailabilities
            .AsNoTracking()
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate <= endDate &&
                (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= startDate))
            .ToListAsync(cancellationToken);

        var dayOffRows = await _dbContext.BusinessUserTimeOffs
            .AsNoTracking()
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate.HasValue &&
                item.EffectiveEndDate.HasValue &&
                item.IsAllDay == true &&
                item.EffectiveStartDate.Value <= endDate &&
                item.EffectiveEndDate.Value >= startDate)
            .ToListAsync(cancellationToken);

        var response = new List<MyBusinessDailyAvailabilityResponse>(totalDays);
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var isDayOff = dayOffRows.Any(item =>
                item.EffectiveStartDate!.Value <= date &&
                item.EffectiveEndDate!.Value >= date);

            if (isDayOff)
            {
                response.Add(new MyBusinessDailyAvailabilityResponse(
                    date,
                    false,
                    Array.Empty<MyBusinessDailyAvailabilityTimeFrameResponse>(),
                    true));
                continue;
            }

            var dayOfWeek = (byte)date.DayOfWeek;
            var matches = availabilityRows
                .Where(item =>
                    item.DayOfWeek == dayOfWeek &&
                    item.EffectiveStartDate <= date &&
                    (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= date))
                .ToList();

            if (matches.Count == 0)
            {
                response.Add(new MyBusinessDailyAvailabilityResponse(
                    date,
                    false,
                    Array.Empty<MyBusinessDailyAvailabilityTimeFrameResponse>(),
                    false));
                continue;
            }

            var effectiveStartDate = matches.Max(item => item.EffectiveStartDate);
            var timeFrames = matches
                .Where(item => item.EffectiveStartDate == effectiveStartDate)
                .OrderBy(item => item.StartTime)
                .Select(item => new MyBusinessDailyAvailabilityTimeFrameResponse(
                    item.StartTime,
                    item.EndTime))
                .ToList();

            response.Add(new MyBusinessDailyAvailabilityResponse(
                date,
                timeFrames.Count > 0,
                timeFrames,
                false));
        }

        return Ok(response);
    }

    [Authorize]
    [HttpGet("{businessCode}/availability/team/daily")]
    public async Task<IActionResult> GetBusinessTeamDailyAvailability(
        string businessCode,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentBusinessUserId = await GetActiveBusinessUserIdAsync(businessId.Value, userId, cancellationToken);
        if (!currentBusinessUserId.HasValue)
        {
            return Forbid();
        }

        var currentHierarchy = await GetActiveBusinessRoleHierarchyNumberAsync(businessId.Value, userId, cancellationToken);
        if (!currentHierarchy.HasValue)
        {
            return Forbid();
        }

        var memberRows = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Join(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRole) => new
                {
                    businessUser.BusinessUserId,
                    businessUser.User.FirstName,
                    businessUser.User.LastName,
                    businessUserRole.BusinessRoleId,
                })
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                item => item.BusinessRoleId,
                role => role.BusinessRoleId,
                (item, role) => new
                {
                    item.BusinessUserId,
                    item.FirstName,
                    item.LastName,
                    role.HierarchyNumber,
                })
            .GroupBy(item => new
            {
                item.BusinessUserId,
                item.FirstName,
                item.LastName,
            })
            .Select(group => new
            {
                group.Key.BusinessUserId,
                group.Key.FirstName,
                group.Key.LastName,
                HierarchyNumber = group.Max(item => item.HierarchyNumber),
            })
            .ToListAsync(cancellationToken);

        var visibleMembers = memberRows
            .Where(member =>
                member.BusinessUserId == currentBusinessUserId.Value ||
                member.HierarchyNumber < currentHierarchy.Value)
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToList();

        var visibleMemberIds = visibleMembers.Select(member => member.BusinessUserId).ToList();
        if (visibleMemberIds.Count == 0)
        {
            return Ok(new BusinessDailyTeamAvailabilityResponse(date, Array.Empty<BusinessDailyTeamAvailabilityMemberResponse>()));
        }

        var dayOfWeek = (byte)date.DayOfWeek;
        var availabilityRows = await _dbContext.BusinessUserAvailabilities
            .AsNoTracking()
            .Where(item =>
                visibleMemberIds.Contains(item.BusinessUserId) &&
                item.DayOfWeek == dayOfWeek &&
                item.EffectiveStartDate <= date &&
                (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= date))
            .ToListAsync(cancellationToken);

        var dayOffMemberIds = await _dbContext.BusinessUserTimeOffs
            .AsNoTracking()
            .Where(item =>
                visibleMemberIds.Contains(item.BusinessUserId) &&
                item.EffectiveStartDate.HasValue &&
                item.EffectiveEndDate.HasValue &&
                item.IsAllDay == true &&
                item.EffectiveStartDate.Value <= date &&
                item.EffectiveEndDate.Value >= date)
            .Select(item => item.BusinessUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var dayOffSet = dayOffMemberIds.ToHashSet();
        var members = new List<BusinessDailyTeamAvailabilityMemberResponse>(visibleMembers.Count);

        foreach (var member in visibleMembers)
        {
            if (dayOffSet.Contains(member.BusinessUserId))
            {
                members.Add(new BusinessDailyTeamAvailabilityMemberResponse(
                    member.BusinessUserId,
                    member.FirstName,
                    member.LastName,
                    false,
                    Array.Empty<BusinessDailyTeamAvailabilityTimeFrameResponse>(),
                    true));
                continue;
            }

            var matches = availabilityRows
                .Where(item => item.BusinessUserId == member.BusinessUserId)
                .ToList();

            if (matches.Count == 0)
            {
                members.Add(new BusinessDailyTeamAvailabilityMemberResponse(
                    member.BusinessUserId,
                    member.FirstName,
                    member.LastName,
                    false,
                    Array.Empty<BusinessDailyTeamAvailabilityTimeFrameResponse>(),
                    false));
                continue;
            }

            var effectiveStartDate = matches.Max(item => item.EffectiveStartDate);
            var frames = matches
                .Where(item => item.EffectiveStartDate == effectiveStartDate)
                .OrderBy(item => item.StartTime)
                .Select(item => new BusinessDailyTeamAvailabilityTimeFrameResponse(item.StartTime, item.EndTime))
                .ToList();

            members.Add(new BusinessDailyTeamAvailabilityMemberResponse(
                member.BusinessUserId,
                member.FirstName,
                member.LastName,
                frames.Count > 0,
                frames,
                false));
        }

        return Ok(new BusinessDailyTeamAvailabilityResponse(date, members));
    }

    [Authorize]
    [HttpPut("{businessCode}/my-availability")]
    public async Task<IActionResult> UpdateMyBusinessAvailability(
        string businessCode,
        UpdateMyBusinessAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var businessUserId = await GetActiveBusinessUserIdAsync(businessId.Value, userId, cancellationToken);
        if (!businessUserId.HasValue)
        {
            return Forbid();
        }

        var availability = (request.Availability ?? Array.Empty<UpdateMyBusinessAvailabilityDayRequest>())
            .OrderBy(item => item.DayOfWeek)
            .ToList();
        var daysOff = (request.DaysOff ?? Array.Empty<UpdateMyBusinessTimeOffRequest>())
            .OrderBy(item => item.EffectiveStartDate)
            .ThenBy(item => item.EffectiveEndDate)
            .ToList();

        if (availability.Any(item => item.DayOfWeek > 6))
        {
            return BadRequest(new { errors = new[] { "Availability day is invalid." } });
        }

        if (availability.Any(item => item.StartTime >= item.EndTime))
        {
            return BadRequest(new { errors = new[] { "Availability start time must be earlier than end time." } });
        }

        if (HasOverlappingAvailabilityRanges(availability))
        {
            return BadRequest(new { errors = new[] { "Availability ranges cannot overlap within a day." } });
        }

        if (daysOff.Any(item => item.EffectiveStartDate > item.EffectiveEndDate))
        {
            return BadRequest(new { errors = new[] { "Day off start date must be on or before the end date." } });
        }

        if (daysOff.Any(item => item.EffectiveStartDate != item.EffectiveEndDate && (item.StartTime != default || item.EndTime != default)))
        {
            return BadRequest(new { errors = new[] { "Multi-day time off must be 12:00 AM to 12:00 AM." } });
        }

        if (daysOff.Any(item => item.StartTime == item.EndTime && item.StartTime != default))
        {
            return BadRequest(new { errors = new[] { "All day time off must be 12:00 AM to 12:00 AM." } });
        }

        if (daysOff.Any(item => item.EffectiveStartDate == item.EffectiveEndDate && item.StartTime != item.EndTime && item.StartTime >= item.EndTime))
        {
            return BadRequest(new { errors = new[] { "Time off start time must be earlier than end time." } });
        }

        if (HasOverlappingTimeOffRanges(daysOff))
        {
            return BadRequest(new { errors = new[] { "Day off ranges cannot overlap." } });
        }

        // If StartTime == EndTime (both midnight), treat as all-day for the date range.
        // Otherwise it's an hourly window and must be single-day (validated above).

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeRows = await _dbContext.BusinessUserAvailabilities
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate <= today &&
                (!item.EffectiveEndDate.HasValue || item.EffectiveEndDate.Value >= today))
            .ToListAsync(cancellationToken);
        var activeAndUpcomingTimeOffRows = await _dbContext.BusinessUserTimeOffs
            .Where(item =>
                item.BusinessUserId == businessUserId.Value &&
                item.EffectiveStartDate.HasValue &&
                item.EffectiveEndDate.HasValue &&
                item.EffectiveEndDate.Value >= today)
            .ToListAsync(cancellationToken);

        var lockedTimeOffRows = activeAndUpcomingTimeOffRows
            .Where(item => item.EffectiveStartDate!.Value <= today)
            .ToList();

        if (daysOff.Any(item => item.EffectiveStartDate <= today))
        {
            return BadRequest(new { errors = new[] { "Time off is locked once it starts. Choose a future start date." } });
        }

        var lockedRequests = lockedTimeOffRows
            .Select(item => new UpdateMyBusinessTimeOffRequest(
                item.EffectiveStartDate!.Value,
                item.EffectiveEndDate!.Value,
                item.EffectiveStartTime ?? default,
                item.EffectiveEndTime ?? default))
            .ToList();

        var combinedTimeOff = lockedRequests.Concat(daysOff).ToList();
        if (HasOverlappingTimeOffRanges(combinedTimeOff))
        {
            return BadRequest(new { errors = new[] { "Day off ranges cannot overlap." } });
        }

        var yesterday = today.AddDays(-1);
        foreach (var row in activeRows)
        {
            row.EffectiveEndDate = yesterday;
        }

        if (activeAndUpcomingTimeOffRows.Count > 0)
        {
            var upcomingRows = activeAndUpcomingTimeOffRows
                .Where(item => item.EffectiveStartDate!.Value > today)
                .ToList();
            if (upcomingRows.Count > 0)
            {
                _dbContext.BusinessUserTimeOffs.RemoveRange(upcomingRows);
            }
        }

        if (availability.Count > 0)
        {
            _dbContext.BusinessUserAvailabilities.AddRange(
                availability.Select(item => new BusinessUserAvailability
                {
                    BusinessUserId = businessUserId.Value,
                    DayOfWeek = item.DayOfWeek,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    EffectiveStartDate = today,
                    EffectiveEndDate = null,
                }));
        }

        if (daysOff.Count > 0)
        {
            var futureDaysOff = daysOff.Where(item => item.EffectiveStartDate > today).ToList();
            _dbContext.BusinessUserTimeOffs.AddRange(
                futureDaysOff.Select(item =>
                {
                    var isAllDay = item.StartTime == item.EndTime;
                    return new BusinessUserTimeOff
                    {
                        BusinessUserId = businessUserId.Value,
                        EffectiveStartDate = item.EffectiveStartDate,
                        EffectiveEndDate = item.EffectiveEndDate,
                        EffectiveStartTime = isAllDay ? null : item.StartTime,
                        EffectiveEndTime = isAllDay ? null : item.EndTime,
                        IsAllDay = isAllDay,
                    };
                }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MyBusinessAvailabilityResponse(
            businessUserId.Value,
            today,
            availability
                .Select(item => new MyBusinessAvailabilityDayResponse(
                    item.DayOfWeek,
                    item.StartTime,
                    item.EndTime))
                .ToList(),
            lockedRequests
                .Select(item => new MyBusinessTimeOffResponse(
                    0,
                    item.EffectiveStartDate,
                    item.EffectiveEndDate,
                    item.StartTime,
                    item.EndTime))
                .Concat(daysOff.Select(item => new MyBusinessTimeOffResponse(
                    0,
                    item.EffectiveStartDate,
                    item.EffectiveEndDate,
                    item.StartTime,
                    item.EndTime)))
                .ToList()));
    }

    [Authorize]
    [HttpGet("{businessCode}/employees")]
    public async Task<IActionResult> GetEmployees(
        string businessCode,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? filters = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        if (pageNumber < 1)
        {
          pageNumber = 1;
        }

        pageSize = Math.Clamp(pageSize, 1, 10);

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .ThenBy(item => item.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null)
        {
            return Forbid();
        }

        var canViewInactiveEmployees = CanManageEmployeeInvites(currentMembership.RoleName);
        if (!canViewInactiveEmployees)
        {
            includeInactive = false;
        }

        var employeeRoleRows = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.BusinessId == businessId.Value && businessUser.EndDate == null)
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
                    user.Email,
                    user.PhoneNumber,
                    user.DateOfBirth,
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
                    joined.businessUser.Email,
                    joined.businessUser.PhoneNumber,
                    joined.businessUser.DateOfBirth,
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
                    joined.joined.Email,
                    joined.joined.PhoneNumber,
                    joined.joined.DateOfBirth,
                    RoleName = businessRole != null ? businessRole.Name : null,
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .ToListAsync(cancellationToken);

        var assignedJobCounts = await _dbContext.Jobs
            .AsNoTracking()
            .Where(job => job.BusinessId == businessId.Value && job.BusinessUserId.HasValue)
            .GroupBy(job => job.BusinessUserId!.Value)
            .Select(group => new { BusinessUserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BusinessUserId, item => item.Count, cancellationToken);

        var filteredEmployees = employeeRoleRows
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

                var roleHierarchyNumber = group
                    .Where(item => item.RoleHierarchyNumber.HasValue)
                    .Select(item => item.RoleHierarchyNumber)
                    .OrderByDescending(value => value)
                    .FirstOrDefault();

                var canDeactivate = CanManageEmployeeInvites(currentMembership.RoleName)
                    && first.IsActive
                    && group.Key != currentMembership.BusinessUserId
                    && currentMembership.RoleHierarchyNumber.HasValue
                    && roleHierarchyNumber.HasValue
                    && roleHierarchyNumber.Value < currentMembership.RoleHierarchyNumber.Value;

                var canUpdateRole = CanManageEmployeeInvites(currentMembership.RoleName)
                    && first.IsActive
                    && group.Key != currentMembership.BusinessUserId
                    && currentMembership.RoleHierarchyNumber.HasValue
                    && roleHierarchyNumber.HasValue
                    && roleHierarchyNumber.Value < currentMembership.RoleHierarchyNumber.Value;

                var sortLastName = first.LastName?.Trim() ?? string.Empty;
                var sortFirstName = first.FirstName?.Trim() ?? string.Empty;

                return new EmployeeListItem(
                    sortLastName,
                    sortFirstName,
                    new BusinessWorkspaceEmployeeResponse(
                        group.Key,
                        string.IsNullOrWhiteSpace(displayName) ? "Team Member" : displayName,
                        first.Email,
                        first.PhoneNumber,
                        first.DateOfBirth,
                        roleName,
                        first.StartDate,
                        first.IsActive,
                        assignedJobCounts.TryGetValue(group.Key, out var assignedJobCount) ? assignedJobCount : 0,
                        canDeactivate,
                        canUpdateRole));
            })
            .ToList();

        var activeFilters = ParseEmployeeFilters(filters);
        if (!includeInactive)
        {
            filteredEmployees = filteredEmployees
                .Where(item => item.Employee.IsActive)
                .ToList();
        }

        foreach (var filterGroup in activeFilters.GroupBy(filter => filter.Field, StringComparer.OrdinalIgnoreCase))
        {
            filteredEmployees = filteredEmployees
                .Where(item => filterGroup.Any(filter => MatchesEmployeeFilter(item, filter)))
                .ToList();
        }

        var normalizedSortBy = (sortBy ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedSortDirection = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var orderedEmployees = normalizedSortBy switch
        {
            "role" => normalizedSortDirection == "desc"
                ? filteredEmployees.OrderByDescending(item => item.Employee.RoleName ?? string.Empty).ThenBy(item => item.Employee.DisplayName).ToList()
                : filteredEmployees.OrderBy(item => item.Employee.RoleName ?? string.Empty).ThenBy(item => item.Employee.DisplayName).ToList(),
            "hireddate" => normalizedSortDirection == "desc"
                ? filteredEmployees.OrderByDescending(item => item.Employee.HiredDate).ThenBy(item => item.Employee.DisplayName).ToList()
                : filteredEmployees.OrderBy(item => item.Employee.HiredDate).ThenBy(item => item.Employee.DisplayName).ToList(),
            "status" => normalizedSortDirection == "desc"
                ? filteredEmployees.OrderByDescending(item => item.Employee.IsActive).ThenBy(item => item.Employee.DisplayName).ToList()
                : filteredEmployees.OrderBy(item => item.Employee.IsActive).ThenBy(item => item.Employee.DisplayName).ToList(),
            "assignedjobcount" => normalizedSortDirection == "desc"
                ? filteredEmployees.OrderByDescending(item => item.Employee.AssignedJobCount).ThenBy(item => item.Employee.DisplayName).ToList()
                : filteredEmployees.OrderBy(item => item.Employee.AssignedJobCount).ThenBy(item => item.Employee.DisplayName).ToList(),
            _ => normalizedSortDirection == "desc"
                ? filteredEmployees
                    .OrderByDescending(item => string.IsNullOrWhiteSpace(item.SortLastName) ? item.Employee.DisplayName : item.SortLastName)
                    .ThenByDescending(item => string.IsNullOrWhiteSpace(item.SortFirstName) ? item.Employee.DisplayName : item.SortFirstName)
                    .ToList()
                : filteredEmployees
                    .OrderBy(item => string.IsNullOrWhiteSpace(item.SortLastName) ? item.Employee.DisplayName : item.SortLastName)
                    .ThenBy(item => string.IsNullOrWhiteSpace(item.SortFirstName) ? item.Employee.DisplayName : item.SortFirstName)
                    .ToList(),
        };

        var employeesOnly = orderedEmployees.Select(item => item.Employee).ToList();

        var totalCount = employeesOnly.Count;
        var employees = employeesOnly
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new BusinessWorkspaceEmployeePageResponse(pageNumber, pageSize, totalCount, employees));
    }

    [Authorize]
    [HttpGet("{businessCode}/employee-invites")]
    public async Task<IActionResult> GetEmployeeInvites(string businessCode, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (currentRoleName is null)
        {
            return Forbid();
        }

        var invites = await _dbContext.EmployeeInviteWorkloads
            .AsNoTracking()
            .Where(item =>
                item.EmployeeInvite.BusinessId == businessId.Value &&
                item.Workload.WorkloadType.Type == WorkloadTypeNames.EmployeeInvite &&
                (
                    item.Workload.WorkloadStatus.Status == WorkloadStatusNames.PendingInvite ||
                    item.Workload.WorkloadStatus.Status == WorkloadStatusNames.Denied
                ))
            .OrderByDescending(item => item.Workload.CreatedDate)
            .Select(item => new BusinessEmployeeInviteResponse(
                item.Workload.WorkloadId,
                item.EmployeeInvite.TargetEmail,
                item.EmployeeInvite.TargetBusinessRole != null ? item.EmployeeInvite.TargetBusinessRole.Name : BusinessRoleNames.Employee,
                BuildDisplayName(
                    item.Workload.CreatedByUser.FirstName,
                    item.Workload.CreatedByUser.LastName,
                    "Workspace User"),
                item.Workload.CreatedByUser.Email,
                item.Workload.CreatedDate,
                item.Workload.WorkloadStatus.Status))
            .ToListAsync(cancellationToken);

        return Ok(invites);
    }

    [Authorize]
    [HttpPost("{businessCode}/employee-invites/{workloadId:long}/close")]
    public async Task<IActionResult> CloseEmployeeInvite(
        string businessCode,
        long workloadId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (!CanManageEmployeeInvites(currentRoleName))
        {
            return Forbid();
        }

        var invite = await _dbContext.EmployeeInviteWorkloads
            .Include(item => item.Workload)
            .Where(item =>
                item.WorkloadId == workloadId &&
                item.EmployeeInvite.BusinessId == businessId.Value &&
                item.Workload.WorkloadType.Type == WorkloadTypeNames.EmployeeInvite &&
                (
                    item.Workload.WorkloadStatus.Status == WorkloadStatusNames.PendingInvite ||
                    item.Workload.WorkloadStatus.Status == WorkloadStatusNames.Denied
                ))
            .SingleOrDefaultAsync(cancellationToken);

        if (invite is null)
        {
            return NotFound(new { errors = new[] { "Employee invite not found." } });
        }

        var closedStatus = await _dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(status =>
                status.WorkloadTypeId == invite.Workload.WorkloadTypeId &&
                status.Status == WorkloadStatusNames.Closed)
            .Select(status => new { status.WorkloadStatusId })
            .SingleOrDefaultAsync(cancellationToken);

        if (closedStatus is null)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { errors = new[] { "Employee invite closed status is unavailable." } });
        }

        invite.Workload.WorkloadStatusId = closedStatus.WorkloadStatusId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("{businessCode}/employee-invites")]
    public async Task<IActionResult> CreateEmployeeInvite(
        string businessCode,
        CreateEmployeeInviteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (!CanManageEmployeeInvites(currentRoleName))
        {
            return Forbid();
        }

        var businessExists = await _dbContext.Businesses
            .AsNoTracking()
            .AnyAsync(
                business => business.BusinessId == businessId.Value && business.InactiveDate == null,
                cancellationToken);

        if (!businessExists)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { errors = new[] { "Email is required." } });
        }

        var roleName = request.RoleName?.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest(new { errors = new[] { "Role is required." } });
        }

        var emailAddressAttribute = new EmailAddressAttribute();
        if (!emailAddressAttribute.IsValid(email))
        {
            return BadRequest(new { errors = new[] { "A valid email address is required." } });
        }

        var normalizedEmail = _userManager.NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest(new { errors = new[] { "A valid email address is required." } });
        }

        var targetBusinessRole = await _dbContext.BusinessRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Name == roleName, cancellationToken);

        if (targetBusinessRole is null)
        {
            return BadRequest(new { errors = new[] { "A valid employee role is required." } });
        }

        var currentRoleHierarchy = await _dbContext.BusinessRoles
            .AsNoTracking()
            .Where(role => role.Name == currentRoleName)
            .Select(role => (long?)role.HierarchyNumber)
            .SingleOrDefaultAsync(cancellationToken);

        if (!currentRoleHierarchy.HasValue || targetBusinessRole.HierarchyNumber > currentRoleHierarchy.Value)
        {
            return Forbid();
        }

        var pendingInviteStatus = await _dbContext.WorkloadStatuses
            .AsNoTracking()
            .Where(workloadStatus =>
                workloadStatus.Status == WorkloadStatusNames.PendingInvite &&
                workloadStatus.WorkloadType.Type == WorkloadTypeNames.EmployeeInvite)
            .Select(workloadStatus => new
            {
                workloadStatus.WorkloadStatusId,
                workloadStatus.WorkloadTypeId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (pendingInviteStatus is null)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { errors = new[] { "Employee invite workflow is unavailable." } });
        }

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.InactiveDate,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (existingUser is null)
        {
            return BadRequest(new { errors = new[] { "That email does not belong to an active Holonix user." } });
        }

        if (existingUser.InactiveDate is not null)
        {
            return BadRequest(new { errors = new[] { "That Holonix user account is inactive." } });
        }

        var isAlreadyActiveEmployee = await _dbContext.BusinessUsers
            .AsNoTracking()
            .AnyAsync(
                businessUser =>
                    businessUser.BusinessId == businessId.Value &&
                    businessUser.UserId == existingUser.Id &&
                    businessUser.IsActive &&
                    businessUser.EndDate == null,
                cancellationToken);

        if (isAlreadyActiveEmployee)
        {
            return BadRequest(new { errors = new[] { "That user is already an active employee of this business." } });
        }

        var pendingInviteExists = await _dbContext.EmployeeInviteWorkloads
            .AsNoTracking()
            .AnyAsync(item =>
                    item.EmployeeInvite.BusinessId == businessId.Value &&
                    item.EmployeeInvite.NormalizedTargetEmail == normalizedEmail &&
                    item.Workload.WorkloadTypeId == pendingInviteStatus.WorkloadTypeId &&
                    item.Workload.WorkloadStatusId == pendingInviteStatus.WorkloadStatusId,
                cancellationToken);

        if (pendingInviteExists)
        {
            return BadRequest(new { errors = new[] { "A pending invite already exists for that email." } });
        }

        var workload = new Workload
        {
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = userId,
            WorkloadTypeId = pendingInviteStatus.WorkloadTypeId,
            WorkloadStatusId = pendingInviteStatus.WorkloadStatusId,
        };

        _dbContext.Workloads.Add(workload);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var employeeInvite = new EmployeeInvite
        {
            BusinessId = businessId.Value,
            TargetEmail = email,
            NormalizedTargetEmail = normalizedEmail,
            TargetUserId = existingUser.Id,
            TargetBusinessRoleId = targetBusinessRole.BusinessRoleId,
        };

        _dbContext.EmployeeInvites.Add(employeeInvite);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.EmployeeInviteWorkloads.Add(new EmployeeInviteWorkload
        {
            WorkloadId = workload.WorkloadId,
            EmployeeInviteId = employeeInvite.EmployeeInviteId,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var currentUser = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.FirstName,
                user.LastName,
                user.Email,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return Ok(new BusinessEmployeeInviteResponse(
            workload.WorkloadId,
            employeeInvite.TargetEmail,
            targetBusinessRole.Name,
            BuildDisplayName(currentUser?.FirstName, currentUser?.LastName, "Workspace User"),
            currentUser?.Email,
            workload.CreatedDate,
            WorkloadStatusNames.PendingInvite));
    }

    [Authorize]
    [HttpPost("{businessCode}/employees/{businessUserId:long}/deactivate")]
    public async Task<IActionResult> DeactivateEmployee(
        string businessCode,
        long businessUserId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .ThenBy(item => item.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null || !CanManageEmployeeInvites(currentMembership.RoleName))
        {
            return Forbid();
        }

        if (currentMembership.BusinessUserId == businessUserId)
        {
            return BadRequest(new { errors = new[] { "You cannot deactivate your own employee record." } });
        }

        var targetMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
                businessUser.BusinessUserId == businessUserId &&
                businessUser.EndDate == null)
            .GroupJoin(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRoles) => new
                {
                    businessUser.BusinessUserId,
                    businessUser.IsActive,
                    businessUserRoles,
                })
            .SelectMany(
                joined => joined.businessUserRoles.DefaultIfEmpty(),
                (joined, businessUserRole) => new
                {
                    joined.BusinessUserId,
                    joined.IsActive,
                    BusinessRoleId = businessUserRole != null ? businessUserRole.BusinessRoleId : (long?)null,
                })
            .GroupJoin(
                _dbContext.BusinessRoles.AsNoTracking(),
                joined => joined.BusinessRoleId,
                businessRole => (long?)businessRole.BusinessRoleId,
                (joined, businessRoles) => new { joined.BusinessUserId, joined.IsActive, businessRoles })
            .SelectMany(
                joined => joined.businessRoles.DefaultIfEmpty(),
                (joined, businessRole) => new
                {
                    joined.BusinessUserId,
                    joined.IsActive,
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetMembership is null)
        {
            return NotFound(new { errors = new[] { "Employee not found." } });
        }

        if (!targetMembership.IsActive)
        {
            return BadRequest(new { errors = new[] { "That employee is already inactive." } });
        }

        if (!currentMembership.RoleHierarchyNumber.HasValue ||
            !targetMembership.RoleHierarchyNumber.HasValue ||
            targetMembership.RoleHierarchyNumber.Value >= currentMembership.RoleHierarchyNumber.Value)
        {
            return Forbid();
        }

        var membership = await _dbContext.BusinessUsers
            .SingleOrDefaultAsync(
                businessUser => businessUser.BusinessId == businessId.Value &&
                                businessUser.BusinessUserId == businessUserId &&
                                businessUser.EndDate == null,
                cancellationToken);

        if (membership is null)
        {
            return NotFound(new { errors = new[] { "Employee not found." } });
        }

        membership.IsActive = false;
        var endedAt = DateTime.UtcNow;

        var activeRoles = await _dbContext.BusinessUserRoles
            .Where(role => role.BusinessUserId == businessUserId && role.EndDate == null)
            .ToListAsync(cancellationToken);

        foreach (var activeRole in activeRoles)
        {
            activeRole.EndDate = endedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("{businessCode}/leave")]
    public async Task<IActionResult> LeaveBusiness(string businessCode, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
                    RoleName = businessUserRole != null ? businessUserRole.BusinessRole.Name : null,
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null)
        {
            return NotFound(new { errors = new[] { "Active business membership not found." } });
        }

        if (string.Equals(currentMembership.RoleName, BusinessRoleNames.Owner, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { errors = new[] { "Business owners cannot leave the business." } });
        }

        var membership = await _dbContext.BusinessUsers
            .SingleOrDefaultAsync(
                businessUser => businessUser.BusinessId == businessId.Value &&
                                businessUser.UserId == userId &&
                                businessUser.IsActive &&
                                businessUser.EndDate == null,
                cancellationToken);

        if (membership is null)
        {
            return NotFound(new { errors = new[] { "Active business membership not found." } });
        }

        membership.IsActive = false;
        var endedAt = DateTime.UtcNow;

        var activeRoles = await _dbContext.BusinessUserRoles
            .Where(role => role.BusinessUserId == membership.BusinessUserId && role.EndDate == null)
            .ToListAsync(cancellationToken);

        foreach (var activeRole in activeRoles)
        {
            activeRole.EndDate = endedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{businessCode}/employees/{businessUserId:long}/role")]
    public async Task<IActionResult> UpdateEmployeeRole(
        string businessCode,
        long businessUserId,
        UpdateEmployeeRoleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .ThenBy(item => item.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentMembership is null || !CanManageEmployeeInvites(currentMembership.RoleName))
        {
            return Forbid();
        }

        if (currentMembership.BusinessUserId == businessUserId)
        {
            return BadRequest(new { errors = new[] { "You cannot update your own role." } });
        }

        var roleName = request.RoleName?.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest(new { errors = new[] { "Role is required." } });
        }

        var targetBusinessRole = await _dbContext.BusinessRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(role => role.Name == roleName, cancellationToken);

        if (targetBusinessRole is null)
        {
            return BadRequest(new { errors = new[] { "A valid employee role is required." } });
        }

        if (!currentMembership.RoleHierarchyNumber.HasValue ||
            targetBusinessRole.HierarchyNumber > currentMembership.RoleHierarchyNumber.Value)
        {
            return Forbid();
        }

        var targetMembership = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
                businessUser.BusinessUserId == businessUserId &&
                businessUser.EndDate == null)
            .GroupJoin(
                _dbContext.BusinessUserRoles.AsNoTracking().Where(role => role.EndDate == null),
                businessUser => businessUser.BusinessUserId,
                businessUserRole => businessUserRole.BusinessUserId,
                (businessUser, businessUserRoles) => new
                {
                    businessUser.BusinessUserId,
                    businessUser.IsActive,
                    businessUserRoles,
                })
            .SelectMany(
                joined => joined.businessUserRoles.DefaultIfEmpty(),
                (joined, businessUserRole) => new
                {
                    joined.BusinessUserId,
                    joined.IsActive,
                    BusinessRoleId = businessUserRole != null ? businessUserRole.BusinessRoleId : (long?)null,
                })
            .GroupJoin(
                _dbContext.BusinessRoles.AsNoTracking(),
                joined => joined.BusinessRoleId,
                businessRole => (long?)businessRole.BusinessRoleId,
                (joined, businessRoles) => new { joined.BusinessUserId, joined.IsActive, businessRoles })
            .SelectMany(
                joined => joined.businessRoles.DefaultIfEmpty(),
                (joined, businessRole) => new
                {
                    joined.BusinessUserId,
                    joined.IsActive,
                    RoleName = businessRole != null ? businessRole.Name : null,
                    RoleHierarchyNumber = businessRole != null ? businessRole.HierarchyNumber : (long?)null,
                })
            .OrderByDescending(item => item.RoleHierarchyNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetMembership is null)
        {
            return NotFound(new { errors = new[] { "Employee not found." } });
        }

        if (!targetMembership.IsActive)
        {
            return BadRequest(new { errors = new[] { "Only active employees can have their role updated." } });
        }

        if (!targetMembership.RoleHierarchyNumber.HasValue ||
            targetMembership.RoleHierarchyNumber.Value >= currentMembership.RoleHierarchyNumber.Value)
        {
            return Forbid();
        }

        if (string.Equals(targetMembership.RoleName, targetBusinessRole.Name, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { errors = new[] { "That employee already has that role." } });
        }

        var activeRoles = await _dbContext.BusinessUserRoles
            .Where(role => role.BusinessUserId == businessUserId && role.EndDate == null)
            .ToListAsync(cancellationToken);

        if (activeRoles.Count == 0)
        {
            return BadRequest(new { errors = new[] { "That employee does not have an active role to update." } });
        }

        var changedAt = DateTime.UtcNow;
        foreach (var activeRole in activeRoles)
        {
            activeRole.EndDate = changedAt;
        }

        _dbContext.BusinessUserRoles.Add(new BusinessUserRole
        {
            BusinessUserId = businessUserId,
            BusinessRoleId = targetBusinessRole.BusinessRoleId,
            StartDate = changedAt,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{businessCode}")]
    public async Task<IActionResult> UpdateBusinessProfile(string businessCode, UpdateBusinessProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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

        if (!IsValidOptionalEmail(request.BusinessEmail))
        {
            return BadRequest(new { errors = new[] { "Business email must be a valid email address." } });
        }

        var business = await _dbContext.Businesses
            .Include(item => item.Details)
            .ThenInclude(details => details!.Country)
            .SingleOrDefaultAsync(item => item.BusinessId == businessId.Value && item.InactiveDate == null, cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        business.Name = request.Name.Trim();
        business.Details ??= new BusinessDetails { BusinessId = business.BusinessId };
        business.Details.Description = NormalizeOptional(request.Description);
        business.Details.BusinessIconBase64 = NormalizeBase64Image(request.BusinessIconBase64);
        business.Details.BusinessEmail = NormalizeOptional(request.BusinessEmail);
        business.Details.BusinessPhoneNumber = NormalizeOptional(request.BusinessPhoneNumber);
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
            business.Details.BusinessEmail,
            business.Details.BusinessPhoneNumber,
            business.Details.Address1,
            business.Details.Address2,
            business.Details.City,
            business.Details.State,
            business.Details.ZipCode,
            business.Details.Country?.Name,
            business.Details.BusinessJobPercentage));
    }

    [Authorize]
    [HttpPut("{businessCode}/services")]
    public async Task<IActionResult> UpdateBusinessServices(string businessCode, UpdateBusinessServicesRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
            .AnyAsync(business => business.BusinessId == businessId.Value && business.InactiveDate == null, cancellationToken);

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
            .Where(item => item.BusinessId == businessId.Value)
            .ToListAsync(cancellationToken);

        var removedServiceIds = existingServices
            .Select(item => item.ServiceId)
            .Except(serviceIds)
            .ToArray();

        _dbContext.BusinessServices.RemoveRange(existingServices);
        _dbContext.BusinessServices.AddRange(
            serviceIds.Select(serviceId => new BusinessService
            {
                BusinessId = businessId.Value,
                ServiceId = serviceId,
            }));

        if (removedServiceIds.Length > 0)
        {
            var endedAt = DateTime.UtcNow;
            await _dbContext.BusinessSubServices
                .Where(item =>
                    item.BusinessId == businessId.Value &&
                    removedServiceIds.Contains(item.ServiceId) &&
                    item.InactiveDate == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.InactiveDate, endedAt), cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var activeSubServices = await GetActiveSubServicesByServiceIdAsync(
            businessId.Value,
            serviceIds,
            cancellationToken);

        return Ok(validServices
            .OrderBy(item => item.Name)
            .Select(item => new BusinessWorkspaceServiceResponse(
                item.ServiceId,
                item.Name,
                activeSubServices.TryGetValue(item.ServiceId, out var subServices)
                    ? subServices
                    : Array.Empty<BusinessWorkspaceSubServiceResponse>()))
            .ToList());
    }

    [Authorize]
    [HttpPost("{businessCode}/services/{serviceId:int}/sub-services")]
    public async Task<IActionResult> CreateBusinessSubService(
        string businessCode,
        int serviceId,
        CreateBusinessSubServiceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (!CanManageBusinessSubServices(currentRoleName))
        {
            return Forbid();
        }

        var normalizedName = NormalizeOptional(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest(new { errors = new[] { "Sub-service name is required." } });
        }

        var parentServiceExists = await _dbContext.BusinessServices
            .AsNoTracking()
            .AnyAsync(
                item => item.BusinessId == businessId.Value && item.ServiceId == serviceId,
                cancellationToken);

        if (!parentServiceExists)
        {
            return NotFound(new { errors = new[] { "Parent service not found for this business." } });
        }

        var duplicateExists = await _dbContext.BusinessSubServices
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.BusinessId == businessId.Value &&
                    item.ServiceId == serviceId &&
                    item.InactiveDate == null &&
                    item.Name == normalizedName,
                cancellationToken);

        if (duplicateExists)
        {
            return BadRequest(new { errors = new[] { "That sub-service already exists." } });
        }

        if (request.Price < 0)
        {
            return BadRequest(new { errors = new[] { "Price cannot be negative." } });
        }

        if (request.Price == 0 && !request.ConsultationNeeded)
        {
            return BadRequest(new { errors = new[] { "Price can only be $0 when consultation is needed." } });
        }

        if (!IsValidSubServiceDuration(request.DurationMinutes))
        {
            return BadRequest(new { errors = new[] { "Duration must be greater than zero and use 15-minute intervals." } });
        }

        if (request.EmployeeCount <= 0)
        {
            return BadRequest(new { errors = new[] { "Employees needed must be a whole number greater than zero." } });
        }

        if (request.EffectiveDate == default)
        {
            return BadRequest(new { errors = new[] { "Effective date is required." } });
        }

        if (request.EffectiveDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { errors = new[] { "Effective date cannot be in the past." } });
        }

        var assignedBusinessUserIds = (request.AssignedBusinessUserIds ?? Array.Empty<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (assignedBusinessUserIds.Length > 0)
        {
            var validAssignedUserIds = await _dbContext.BusinessUsers
                .AsNoTracking()
                .Where(item =>
                    item.BusinessId == businessId.Value &&
                    item.IsActive &&
                    item.EndDate == null &&
                    assignedBusinessUserIds.Contains(item.BusinessUserId))
                .Select(item => item.BusinessUserId)
                .ToListAsync(cancellationToken);

            if (validAssignedUserIds.Count != assignedBusinessUserIds.Length)
            {
                return BadRequest(new { errors = new[] { "One or more assigned users are not active employees for this business." } });
            }
        }

        var subService = new BusinessSubService
        {
            BusinessId = businessId.Value,
            ServiceId = serviceId,
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            ConsultationNeeded = request.ConsultationNeeded,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            EmployeeCount = request.EmployeeCount,
            EffectiveDate = request.EffectiveDate,
        };

        _dbContext.BusinessSubServices.Add(subService);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (assignedBusinessUserIds.Length > 0)
        {
            _dbContext.BusinessSubServiceAssignments.AddRange(
                assignedBusinessUserIds.Select(businessUserId => new BusinessSubServiceAssignment
                {
                    BusinessSubServiceId = subService.BusinessSubServiceId,
                    BusinessUserId = businessUserId,
                }));

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var createdSubServices = await GetActiveSubServicesByServiceIdAsync(
            businessId.Value,
            new[] { serviceId },
            cancellationToken);

        var createdSubService = createdSubServices.TryGetValue(serviceId, out var createdEntries)
            ? createdEntries.SingleOrDefault(item => item.BusinessSubServiceId == subService.BusinessSubServiceId)
            : null;

        return Ok(createdSubService ?? new BusinessWorkspaceSubServiceResponse(
            subService.BusinessSubServiceId,
            subService.Name,
            subService.Description,
            subService.ConsultationNeeded,
            subService.DurationMinutes,
            subService.Price,
            subService.EmployeeCount,
            subService.EffectiveDate,
            Array.Empty<BusinessWorkspaceSubServiceAssignmentResponse>()));
    }

    [Authorize]
    [HttpDelete("{businessCode}/sub-services/{businessSubServiceId:long}")]
    public async Task<IActionResult> DeleteBusinessSubService(
        string businessCode,
        long businessSubServiceId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (!CanManageBusinessSubServices(currentRoleName))
        {
            return Forbid();
        }

        var subService = await _dbContext.BusinessSubServices
            .SingleOrDefaultAsync(
                item =>
                    item.BusinessSubServiceId == businessSubServiceId &&
                    item.BusinessId == businessId.Value &&
                    item.InactiveDate == null,
                cancellationToken);

        if (subService is null)
        {
            return NotFound(new { errors = new[] { "Sub-service not found." } });
        }

        subService.InactiveDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{businessCode}/sub-services/{businessSubServiceId:long}")]
    public async Task<IActionResult> UpdateBusinessSubService(
        string businessCode,
        long businessSubServiceId,
        UpdateBusinessSubServiceRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var currentRoleName = await GetActiveBusinessRoleNameAsync(businessId.Value, userId, cancellationToken);
        if (!CanManageBusinessSubServices(currentRoleName))
        {
            return Forbid();
        }

        var subService = await _dbContext.BusinessSubServices
            .SingleOrDefaultAsync(
                item =>
                    item.BusinessSubServiceId == businessSubServiceId &&
                    item.BusinessId == businessId.Value &&
                    item.InactiveDate == null,
                cancellationToken);

        if (subService is null)
        {
            return NotFound(new { errors = new[] { "Sub-service not found." } });
        }

        var normalizedName = NormalizeOptional(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest(new { errors = new[] { "Sub-service name is required." } });
        }

        if (request.Price < 0)
        {
            return BadRequest(new { errors = new[] { "Price cannot be negative." } });
        }

        if (request.Price == 0 && !request.ConsultationNeeded)
        {
            return BadRequest(new { errors = new[] { "Price can only be $0 when consultation is needed." } });
        }

        if (!IsValidSubServiceDuration(request.DurationMinutes))
        {
            return BadRequest(new { errors = new[] { "Duration must be greater than zero and use 15-minute intervals." } });
        }

        if (request.EmployeeCount <= 0)
        {
            return BadRequest(new { errors = new[] { "Employees needed must be a whole number greater than zero." } });
        }

        if (request.EffectiveDate == default)
        {
            return BadRequest(new { errors = new[] { "Effective date is required." } });
        }

        if (request.EffectiveDate < DateOnly.FromDateTime(DateTime.UtcNow)
            && request.EffectiveDate != subService.EffectiveDate)
        {
            return BadRequest(new { errors = new[] { "Effective date cannot be in the past." } });
        }

        var parentServiceExists = await _dbContext.BusinessServices
            .AsNoTracking()
            .AnyAsync(item => item.BusinessId == businessId.Value && item.ServiceId == request.ServiceId, cancellationToken);

        if (!parentServiceExists)
        {
            return NotFound(new { errors = new[] { "Parent service not found for this business." } });
        }

        var duplicateExists = await _dbContext.BusinessSubServices
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.BusinessSubServiceId != businessSubServiceId &&
                    item.BusinessId == businessId.Value &&
                    item.ServiceId == request.ServiceId &&
                    item.InactiveDate == null &&
                    item.Name == normalizedName,
                cancellationToken);

        if (duplicateExists)
        {
            return BadRequest(new { errors = new[] { "That sub-service already exists." } });
        }

        var assignedBusinessUserIds = (request.AssignedBusinessUserIds ?? Array.Empty<long>())
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (assignedBusinessUserIds.Length > 0)
        {
            var validAssignedUserIds = await _dbContext.BusinessUsers
                .AsNoTracking()
                .Where(item =>
                    item.BusinessId == businessId.Value &&
                    item.IsActive &&
                    item.EndDate == null &&
                    assignedBusinessUserIds.Contains(item.BusinessUserId))
                .Select(item => item.BusinessUserId)
                .ToListAsync(cancellationToken);

            if (validAssignedUserIds.Count != assignedBusinessUserIds.Length)
            {
                return BadRequest(new { errors = new[] { "One or more assigned users are not active employees for this business." } });
            }
        }

        subService.Name = normalizedName;
        subService.ServiceId = request.ServiceId;
        subService.Description = NormalizeOptional(request.Description);
        subService.ConsultationNeeded = request.ConsultationNeeded;
        subService.DurationMinutes = request.DurationMinutes;
        subService.Price = request.Price;
        subService.EmployeeCount = request.EmployeeCount;
        subService.EffectiveDate = request.EffectiveDate;

        var existingAssignments = await _dbContext.BusinessSubServiceAssignments
            .Where(item => item.BusinessSubServiceId == businessSubServiceId)
            .ToListAsync(cancellationToken);

        _dbContext.BusinessSubServiceAssignments.RemoveRange(existingAssignments);
        if (assignedBusinessUserIds.Length > 0)
        {
            _dbContext.BusinessSubServiceAssignments.AddRange(
                assignedBusinessUserIds.Select(businessUserId => new BusinessSubServiceAssignment
                {
                    BusinessSubServiceId = businessSubServiceId,
                    BusinessUserId = businessUserId,
                }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedSubServices = await GetActiveSubServicesByServiceIdAsync(
            businessId.Value,
            new[] { request.ServiceId },
            cancellationToken);

        var updatedSubService = updatedSubServices.TryGetValue(request.ServiceId, out var updatedEntries)
            ? updatedEntries.SingleOrDefault(item => item.BusinessSubServiceId == businessSubServiceId)
            : null;

        return Ok(updatedSubService ?? new BusinessWorkspaceSubServiceResponse(
            subService.BusinessSubServiceId,
            subService.Name,
            subService.Description,
            subService.ConsultationNeeded,
            subService.DurationMinutes,
            subService.Price,
            subService.EmployeeCount,
            subService.EffectiveDate,
            Array.Empty<BusinessWorkspaceSubServiceAssignmentResponse>()));
    }

    [Authorize]
    [HttpDelete("{businessCode}")]
    public async Task<IActionResult> DeleteBusiness(string businessCode, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var businessId = await GetActiveBusinessIdByCodeAsync(businessCode, cancellationToken);
        if (!businessId.HasValue)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var isOwner = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId.Value &&
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
            .SingleOrDefaultAsync(item => item.BusinessId == businessId.Value && item.InactiveDate == null, cancellationToken);

        if (business is null)
        {
            return NotFound(new { errors = new[] { "Business not found." } });
        }

        var businessUserIds = await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser => businessUser.BusinessId == businessId.Value)
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
                .Where(item => item.BusinessId == businessId.Value && (item.IsActive || item.EndDate == null))
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
            BusinessCode = await GenerateUniqueBusinessCodeAsync(cancellationToken),
            Name = request.Name.Trim(),
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsProductBased = request.IsProductBased,
            IsRecurring = false,
            Details = new BusinessDetails
            {
                Description = NormalizeOptional(request.Description),
                BusinessEmail = NormalizeOptional(request.BusinessEmail),
                BusinessPhoneNumber = NormalizeOptional(request.BusinessPhoneNumber),
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
            business.BusinessCode,
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

        if (!IsValidOptionalEmail(request.BusinessEmail))
        {
            errors.Add("Business email must be a valid email address.");
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

    private static bool IsValidOptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return new EmailAddressAttribute().IsValid(value.Trim());
    }

    private async Task<int?> GetActiveBusinessIdByCodeAsync(string businessCode, CancellationToken cancellationToken)
    {
        var normalizedBusinessCode = NormalizeBusinessCode(businessCode);
        if (normalizedBusinessCode is not null)
        {
            return await _dbContext.Businesses
                .AsNoTracking()
                .Where(business => business.BusinessCode == normalizedBusinessCode && business.InactiveDate == null)
                .Select(business => (int?)business.BusinessId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (int.TryParse(businessCode?.Trim(), out var legacyBusinessId) && legacyBusinessId > 0)
        {
            return await _dbContext.Businesses
                .AsNoTracking()
                .Where(business => business.BusinessId == legacyBusinessId && business.InactiveDate == null)
                .Select(business => (int?)business.BusinessId)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return null;
    }

    private static string? NormalizeBusinessCode(string? businessCode)
    {
        if (string.IsNullOrWhiteSpace(businessCode))
        {
            return null;
        }

        var normalized = businessCode.Trim().ToUpperInvariant();
        return normalized.Length == BusinessCodeLength ? normalized : null;
    }

    private async Task<string> GenerateUniqueBusinessCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var businessCode = GenerateBusinessCode();
            var exists = await _dbContext.Businesses
                .AsNoTracking()
                .AnyAsync(business => business.BusinessCode == businessCode, cancellationToken);

            if (!exists)
            {
                return businessCode;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique business code.");
    }

    private static string GenerateBusinessCode()
    {
        Span<byte> buffer = stackalloc byte[BusinessCodeLength];
        RandomNumberGenerator.Fill(buffer);

        Span<char> chars = stackalloc char[BusinessCodeLength];
        for (var index = 0; index < buffer.Length; index++)
        {
            chars[index] = BusinessCodeCharacters[buffer[index] % BusinessCodeCharacters.Length];
        }

        return new string(chars);
    }

    private async Task<Dictionary<int, IReadOnlyList<BusinessWorkspaceSubServiceResponse>>> GetActiveSubServicesByServiceIdAsync(
        int businessId,
        IReadOnlyCollection<int> serviceIds,
        CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0)
        {
            return [];
        }

        var subServices = await _dbContext.BusinessSubServices
            .AsNoTracking()
            .Where(item =>
                item.BusinessId == businessId &&
                item.InactiveDate == null &&
                serviceIds.Contains(item.ServiceId))
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var subServiceIds = subServices
            .Select(item => item.BusinessSubServiceId)
            .ToArray();

        Dictionary<long, IReadOnlyList<BusinessWorkspaceSubServiceAssignmentResponse>> assignedUsersBySubServiceId;
        if (subServiceIds.Length == 0)
        {
            assignedUsersBySubServiceId = [];
        }
        else
        {
            var assignedUserRows = await _dbContext.BusinessSubServiceAssignments
                .AsNoTracking()
                .Where(item => subServiceIds.Contains(item.BusinessSubServiceId))
                .Join(
                    _dbContext.BusinessUsers.AsNoTracking().Where(item => item.IsActive && item.EndDate == null),
                    assignment => assignment.BusinessUserId,
                    businessUser => businessUser.BusinessUserId,
                    (assignment, businessUser) => new
                    {
                        assignment.BusinessSubServiceId,
                        businessUser.BusinessUserId,
                        businessUser.UserId,
                    })
                .Join(
                    _dbContext.Users.AsNoTracking(),
                    assignment => assignment.UserId,
                    user => user.Id,
                    (assignment, user) => new
                    {
                        assignment.BusinessSubServiceId,
                        assignment.BusinessUserId,
                        user.FirstName,
                        user.LastName,
                    })
                .ToListAsync(cancellationToken);

            assignedUsersBySubServiceId = assignedUserRows
                .GroupBy(item => item.BusinessSubServiceId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<BusinessWorkspaceSubServiceAssignmentResponse>)group
                        .OrderBy(item => item.LastName)
                        .ThenBy(item => item.FirstName)
                        .Select(item => new BusinessWorkspaceSubServiceAssignmentResponse(
                            item.BusinessUserId,
                            BuildDisplayName(item.FirstName, item.LastName, "Team Member")))
                        .ToList());
        }

        return subServices
            .GroupBy(item => item.ServiceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<BusinessWorkspaceSubServiceResponse>)group
                    .Select(item => new BusinessWorkspaceSubServiceResponse(
                        item.BusinessSubServiceId,
                        item.Name,
                        item.Description,
                        item.ConsultationNeeded,
                        item.DurationMinutes,
                        item.Price,
                        item.EmployeeCount,
                        item.EffectiveDate,
                        assignedUsersBySubServiceId.TryGetValue(item.BusinessSubServiceId, out var assignedUsers)
                            ? assignedUsers
                            : Array.Empty<BusinessWorkspaceSubServiceAssignmentResponse>()))
                    .ToList());
    }

    private async Task<string?> GetActiveBusinessRoleNameAsync(
        int businessId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BusinessUsers
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
                (_, businessUserRole) => businessUserRole.BusinessRoleId)
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                businessRoleId => businessRoleId,
                businessRole => businessRole.BusinessRoleId,
                (_, businessRole) => new
                {
                    businessRole.Name,
                    businessRole.HierarchyNumber,
                })
            .OrderByDescending(businessRole => businessRole.HierarchyNumber)
            .Select(businessRole => businessRole.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<long?> GetActiveBusinessRoleHierarchyNumberAsync(
        int businessId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BusinessUsers
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
                (_, businessUserRole) => businessUserRole.BusinessRoleId)
            .Join(
                _dbContext.BusinessRoles.AsNoTracking(),
                businessRoleId => businessRoleId,
                businessRole => businessRole.BusinessRoleId,
                (_, businessRole) => businessRole.HierarchyNumber)
            .OrderByDescending(hierarchyNumber => hierarchyNumber)
            .Select(hierarchyNumber => (long?)hierarchyNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<long?> GetActiveBusinessUserIdAsync(
        int businessId,
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BusinessUsers
            .AsNoTracking()
            .Where(businessUser =>
                businessUser.BusinessId == businessId &&
                businessUser.UserId == userId &&
                businessUser.IsActive &&
                businessUser.EndDate == null)
            .Select(businessUser => (long?)businessUser.BusinessUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool CanManageEmployeeInvites(string? roleName)
    {
        return string.Equals(roleName, BusinessRoleNames.Owner, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, BusinessRoleNames.Administrator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanManageBusinessSubServices(string? roleName)
    {
        return string.Equals(roleName, BusinessRoleNames.Owner, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, BusinessRoleNames.Administrator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidSubServiceDuration(int durationMinutes)
    {
        return durationMinutes > 0 && durationMinutes % 15 == 0;
    }

    private static bool HasOverlappingTimeOffRanges(IReadOnlyList<UpdateMyBusinessTimeOffRequest> daysOff)
    {
        var allDay = daysOff
            .Where(item => item.StartTime == item.EndTime)
            .OrderBy(item => item.EffectiveStartDate)
            .ThenBy(item => item.EffectiveEndDate)
            .ToList();

        for (var index = 1; index < allDay.Count; index++)
        {
            var previous = allDay[index - 1];
            var current = allDay[index];
            if (current.EffectiveStartDate <= previous.EffectiveEndDate)
            {
                return true;
            }
        }

        var hourly = daysOff
            .Where(item => item.StartTime != item.EndTime)
            .Select(item => new
            {
                Date = item.EffectiveStartDate,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
            })
            .ToList();

        if (hourly.Count > 0 && allDay.Count > 0)
        {
            foreach (var entry in hourly)
            {
                if (allDay.Any(range => range.EffectiveStartDate <= entry.Date && range.EffectiveEndDate >= entry.Date))
                {
                    return true;
                }
            }
        }

        foreach (var group in hourly.GroupBy(item => item.Date))
        {
            var ordered = group
                .OrderBy(item => item.StartTime)
                .ThenBy(item => item.EndTime)
                .ToList();

            for (var index = 1; index < ordered.Count; index++)
            {
                var previous = ordered[index - 1];
                var current = ordered[index];
                if (current.StartTime < previous.EndTime)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasOverlappingAvailabilityRanges(IReadOnlyList<UpdateMyBusinessAvailabilityDayRequest> availability)
    {
        if (availability.Count <= 1)
        {
            return false;
        }

        foreach (var group in availability.GroupBy(item => item.DayOfWeek))
        {
            var ordered = group
                .OrderBy(item => item.StartTime)
                .ThenBy(item => item.EndTime)
                .ToList();

            for (var index = 1; index < ordered.Count; index++)
            {
                var previous = ordered[index - 1];
                var current = ordered[index];
                if (current.StartTime < previous.EndTime)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<EmployeeFilterQuery> ParseEmployeeFilters(string? filters)
    {
        if (string.IsNullOrWhiteSpace(filters))
        {
            return [];
        }

        return filters
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(segment => segment.Split('~', 3, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 3)
            .Select(parts => new EmployeeFilterQuery(parts[0], parts[1], Uri.UnescapeDataString(parts[2])))
            .Where(filter => !string.IsNullOrWhiteSpace(filter.Value))
            .ToList();
    }

    private static bool MatchesEmployeeFilter(EmployeeListItem employee, EmployeeFilterQuery filter)
    {
        var field = filter.Field.Trim().ToLowerInvariant();
        var op = filter.Operator.Trim().ToLowerInvariant();
        var values = filter.Value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToList();

        if (values.Count == 0)
        {
            return false;
        }

        var candidate = field switch
        {
            "employee" => employee.Employee.DisplayName as string ?? string.Empty,
            "role" => employee.Employee.RoleName as string ?? string.Empty,
            "status" => employee.Employee.IsActive ? "Active" : "Inactive",
            _ => string.Empty,
        };

        return op switch
        {
            "is" => values.Any(value => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase)),
            _ => values.Any(value => candidate.Contains(value, StringComparison.OrdinalIgnoreCase)),
        };
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string fallback)
    {
        var displayName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? fallback : displayName;
    }

    private sealed record EmployeeListItem(
        string SortLastName,
        string SortFirstName,
        BusinessWorkspaceEmployeeResponse Employee);

    private sealed record EmployeeFilterQuery(string Field, string Operator, string Value);
}
