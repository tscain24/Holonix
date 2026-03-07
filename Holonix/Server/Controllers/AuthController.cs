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

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var token = _tokenService.CreateToken(user);
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

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var businessUsers = await _dbContext.BusinessUsers
            .Where(x => x.UserId == userId)
            .Select(x => x.BusinessUserId)
            .ToListAsync(cancellationToken);

        var linkedJobs = await _dbContext.Jobs
            .Where(job => job.UserId == userId || (job.BusinessUserId.HasValue && businessUsers.Contains(job.BusinessUserId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var job in linkedJobs)
        {
            if (job.UserId == userId)
            {
                job.UserId = null;
            }

            if (job.BusinessUserId.HasValue && businessUsers.Contains(job.BusinessUserId.Value))
            {
                job.BusinessUserId = null;
            }
        }

        if (businessUsers.Count > 0)
        {
            var businessUserRoles = await _dbContext.BusinessUserRoles
                .Where(x => businessUsers.Contains(x.BusinessUserId))
                .ToListAsync(cancellationToken);
            var businessUserAvailabilities = await _dbContext.BusinessUserAvailabilities
                .Where(x => businessUsers.Contains(x.BusinessUserId))
                .ToListAsync(cancellationToken);
            var businessUserTimeOffs = await _dbContext.BusinessUserTimeOffs
                .Where(x => businessUsers.Contains(x.BusinessUserId))
                .ToListAsync(cancellationToken);
            var businessUserEntities = await _dbContext.BusinessUsers
                .Where(x => businessUsers.Contains(x.BusinessUserId))
                .ToListAsync(cancellationToken);

            _dbContext.BusinessUserRoles.RemoveRange(businessUserRoles);
            _dbContext.BusinessUserAvailabilities.RemoveRange(businessUserAvailabilities);
            _dbContext.BusinessUserTimeOffs.RemoveRange(businessUserTimeOffs);
            _dbContext.BusinessUsers.RemoveRange(businessUserEntities);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = await _userManager.DeleteAsync(user);
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

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        var response = new UserProfileResponse(
            user.FirstName,
            user.LastName,
            user.DateOfBirth?.ToString("yyyy-MM-dd"),
            user.Email ?? string.Empty,
            user.ProfileImageBase64);

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

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new { errors = new[] { "User not found." } });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DateOfBirth = parsedDob;

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
            user.ProfileImageBase64);

        return Ok(response);
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

        var user = await _userManager.FindByIdAsync(userId);
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
}
