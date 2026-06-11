using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Contracts.Auth;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;

namespace Holonix.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IRegisterUserHandler _registerHandler;
    private readonly ILoginUserHandler _loginHandler;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _dbContext;

    public AuthController(
        IRegisterUserHandler registerHandler,
        ILoginUserHandler loginHandler,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ApplicationDbContext dbContext)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerHandler.HandleAsync(request, cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { errors = result.Errors });
        }

        return Ok(result.Payload);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginHandler.HandleAsync(request, cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { errors = result.Errors });
        }

        return Ok(result.Payload);
    }

    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var token = await _tokenService.CreateTokenAsync(user, cancellationToken);
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return Ok(new AuthResponse(user.Id, displayName, token, user.ProfileImageBase64));
    }

    [Authorize]
    [HttpDelete("profile")]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var endedAt = DateTime.UtcNow;
        var businessUsers = await _dbContext.BusinessUsers
            .Where(x => x.UserId == userId)
            .Select(x => x.BusinessUserId)
            .ToListAsync(cancellationToken);

        if (businessUsers.Count > 0)
        {
            await _dbContext.BusinessUserRoles
                .Where(x => businessUsers.Contains(x.BusinessUserId) && x.EndDate == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(role => role.EndDate, endedAt),
                    cancellationToken);

            await _dbContext.BusinessUsers
                .Where(x => businessUsers.Contains(x.BusinessUserId) && (x.IsActive || x.EndDate == null))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(businessUser => businessUser.IsActive, false)
                        .SetProperty(businessUser => businessUser.EndDate, endedAt),
                    cancellationToken);
        }

        user.InactiveDate = endedAt;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.UserName = $"deleted::{user.Id}::{Guid.NewGuid():N}";
        user.NormalizedUserName = _userManager.NormalizeName(user.UserName);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToArray();
            return BadRequest(new { errors });
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        await EnsureLegacySavedLocationAsync(user.Id, cancellationToken);
        var locations = await GetActiveSavedLocationsAsync(user.Id, cancellationToken);

        var response = new UserProfileResponse(
            user.FirstName,
            user.LastName,
            user.DateOfBirth?.ToString("yyyy-MM-dd"),
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.ProfileImageBase64,
            locations);

        return Ok(response);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new { errors = new[] { "First and last name are required." } });
        }

        DateOnly? parsedDob = null;
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (!DateOnly.TryParse(request.DateOfBirth, out var dob))
            {
                return BadRequest(new { errors = new[] { "Date of birth must be YYYY-MM-DD." } });
            }

            parsedDob = dob;
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DateOfBirth = parsedDob;
        user.PhoneNumber = NormalizePhoneNumber(request.PhoneNumber);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(error => error.Description).ToArray();
            return BadRequest(new { errors });
        }

        var response = new UserProfileResponse(
            user.FirstName,
            user.LastName,
            user.DateOfBirth?.ToString("yyyy-MM-dd"),
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.ProfileImageBase64,
            await GetActiveSavedLocationsAsync(user.Id, cancellationToken));

        return Ok(response);
    }

    [Authorize]
    [HttpPost("profile/locations")]
    public async Task<IActionResult> CreateSavedLocation(CreateUserSavedLocationRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var userExists = await _userManager.Users
            .AsNoTracking()
            .AnyAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (!userExists)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var errors = ValidateSavedLocationRequest(request.Label, request.Address1, request.City, request.State, request.ZipCode, request.Latitude, request.Longitude);
        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        await EnsureLegacySavedLocationAsync(userId, cancellationToken);

        var hasActiveLocations = await _dbContext.UserSavedLocations
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId && item.InactiveDate == null, cancellationToken);
        var shouldBePrimary = request.IsPrimary || !hasActiveLocations;

        if (shouldBePrimary)
        {
            await ClearPrimarySavedLocationsAsync(userId, cancellationToken);
        }

        var location = new UserSavedLocation
        {
            UserId = userId,
            Label = request.Label.Trim(),
            Address1 = request.Address1.Trim(),
            Address2 = NormalizeOptional(request.Address2),
            City = request.City.Trim(),
            State = request.State.Trim(),
            ZipCode = request.ZipCode.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsPrimary = shouldBePrimary,
            CreatedDate = DateTime.UtcNow,
        };

        _dbContext.UserSavedLocations.Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToSavedLocationResponse(location));
    }

    [Authorize]
    [HttpPut("profile/locations/{userSavedLocationId:long}")]
    public async Task<IActionResult> UpdateSavedLocation(
        long userSavedLocationId,
        UpdateUserSavedLocationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var errors = ValidateSavedLocationRequest(request.Label, request.Address1, request.City, request.State, request.ZipCode, request.Latitude, request.Longitude);
        if (errors.Count > 0)
        {
            return BadRequest(new { errors });
        }

        await EnsureLegacySavedLocationAsync(userId, cancellationToken);

        var location = await _dbContext.UserSavedLocations
            .SingleOrDefaultAsync(
                item => item.UserSavedLocationId == userSavedLocationId && item.UserId == userId && item.InactiveDate == null,
                cancellationToken);

        if (location is null)
        {
            return NotFound(new { errors = new[] { "Saved location not found." } });
        }

        if (request.IsPrimary)
        {
            await ClearPrimarySavedLocationsAsync(userId, cancellationToken, userSavedLocationId);
        }

        location.Label = request.Label.Trim();
        location.Address1 = request.Address1.Trim();
        location.Address2 = NormalizeOptional(request.Address2);
        location.City = request.City.Trim();
        location.State = request.State.Trim();
        location.ZipCode = request.ZipCode.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.IsPrimary = request.IsPrimary;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToSavedLocationResponse(location));
    }

    [Authorize]
    [HttpDelete("profile/locations/{userSavedLocationId:long}")]
    public async Task<IActionResult> DeleteSavedLocation(long userSavedLocationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        await EnsureLegacySavedLocationAsync(userId, cancellationToken);

        var location = await _dbContext.UserSavedLocations
            .SingleOrDefaultAsync(
                item => item.UserSavedLocationId == userSavedLocationId && item.UserId == userId && item.InactiveDate == null,
                cancellationToken);

        if (location is null)
        {
            return NotFound(new { errors = new[] { "Saved location not found." } });
        }

        location.InactiveDate = DateTime.UtcNow;
        location.IsPrimary = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var remainingLocations = await _dbContext.UserSavedLocations
            .Where(item => item.UserId == userId && item.InactiveDate == null)
            .OrderBy(item => item.CreatedDate)
            .ToListAsync(cancellationToken);

        if (remainingLocations.Count > 0 && remainingLocations.All(item => !item.IsPrimary))
        {
            remainingLocations[0].IsPrimary = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [Authorize]
    [HttpPut("profile-image")]
    public async Task<IActionResult> UpdateProfileImage(UpdateProfileImageRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { errors = new[] { "Invalid user context." } });
        }

        var user = await _userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.InactiveDate == null, cancellationToken);

        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        user.ProfileImageBase64 = NormalizeBase64Image(request.ProfileImageBase64);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToArray();
            return BadRequest(new { errors });
        }

        return Ok(new { profileImageBase64 = user.ProfileImageBase64 });
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

    private static string? NormalizePhoneNumber(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task EnsureLegacySavedLocationAsync(string userId, CancellationToken cancellationToken)
    {
        var hasSavedLocations = await _dbContext.UserSavedLocations
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId && item.InactiveDate == null, cancellationToken);

        if (hasSavedLocations)
        {
            return;
        }

        var legacyAddress = await _dbContext.UserAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (legacyAddress is null)
        {
            return;
        }

        _dbContext.UserSavedLocations.Add(new UserSavedLocation
        {
            UserId = userId,
            Label = "Home",
            Address1 = legacyAddress.Address1,
            Address2 = legacyAddress.Address2,
            City = legacyAddress.City,
            State = legacyAddress.State,
            ZipCode = legacyAddress.ZipCode,
            Latitude = legacyAddress.Latitude,
            Longitude = legacyAddress.Longitude,
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<UserSavedLocationResponse>> GetActiveSavedLocationsAsync(string userId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserSavedLocations
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.InactiveDate == null)
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.CreatedDate)
            .Select(item => new UserSavedLocationResponse(
                item.UserSavedLocationId,
                item.Label,
                item.Address1,
                item.Address2,
                item.City,
                item.State,
                item.ZipCode,
                item.Latitude,
                item.Longitude,
                item.IsPrimary))
            .ToListAsync(cancellationToken);
    }

    private async Task ClearPrimarySavedLocationsAsync(string userId, CancellationToken cancellationToken, long? excludeUserSavedLocationId = null)
    {
        var locations = await _dbContext.UserSavedLocations
            .Where(item =>
                item.UserId == userId &&
                item.InactiveDate == null &&
                item.IsPrimary &&
                (!excludeUserSavedLocationId.HasValue || item.UserSavedLocationId != excludeUserSavedLocationId.Value))
            .ToListAsync(cancellationToken);

        foreach (var location in locations)
        {
            location.IsPrimary = false;
        }
    }

    private static UserSavedLocationResponse ToSavedLocationResponse(UserSavedLocation item)
    {
        return new UserSavedLocationResponse(
            item.UserSavedLocationId,
            item.Label,
            item.Address1,
            item.Address2,
            item.City,
            item.State,
            item.ZipCode,
            item.Latitude,
            item.Longitude,
            item.IsPrimary);
    }

    private static List<string> ValidateSavedLocationRequest(
        string? label,
        string? address1,
        string? city,
        string? state,
        string? zipCode,
        decimal? latitude,
        decimal? longitude)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add("Location label is required.");
        }

        if (string.IsNullOrWhiteSpace(address1)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(zipCode))
        {
            errors.Add("Address, city, state, and zip code are required.");
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            errors.Add("Saved locations must be selected from a valid address result.");
        }

        if (latitude is < -90 or > 90)
        {
            errors.Add("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            errors.Add("Longitude must be between -180 and 180.");
        }

        return errors;
    }
}
