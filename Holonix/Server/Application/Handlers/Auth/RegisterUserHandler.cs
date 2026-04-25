using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Data;
using System.Data.Common;

namespace Holonix.Server.Application.Handlers.Auth;

public sealed class RegisterUserHandler : IRegisterUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger<RegisterUserHandler> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HandlerResult<RegisterResponse>> HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "Email and password are required.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "First and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Address1)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.State)
            || string.IsNullOrWhiteSpace(request.ZipCode))
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "Address, city, state, and zip code are required.");
        }

        if (request.Latitude is < -90 or > 90)
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "Latitude must be between -90 and 90.");
        }

        if (request.Longitude is < -180 or > 180)
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "Longitude must be between -180 and 180.");
        }

        DateOnly? dateOfBirth = null;
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (!DateOnly.TryParse(request.DateOfBirth, out var parsedDob))
            {
                return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, "Date of birth must be YYYY-MM-DD.");
            }

            dateOfBirth = parsedDob;
        }

        var normalizedEmail = _userManager.NormalizeEmail(request.Email.Trim());
        ApplicationUser? existingUser;
        try
        {
            existingUser = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.NormalizedEmail == normalizedEmail && user.InactiveDate == null,
                    cancellationToken);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Database unavailable while checking for existing email during registration.");
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while checking for existing email during registration.");
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }

        if (existingUser is not null)
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status409Conflict, "Email already in use.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
            DateOfBirth = dateOfBirth,
            ProfileImageBase64 = NormalizeBase64Image(request.ProfileImageBase64),
        };

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, request.Password);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Database unavailable while creating user during registration.");
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while creating user during registration.");
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, errors);
        }

        try
        {
            var address = new UserAddress
            {
                UserId = user.Id,
                Address1 = request.Address1.Trim(),
                Address2 = string.IsNullOrWhiteSpace(request.Address2) ? null : request.Address2.Trim(),
                City = request.City.Trim(),
                State = request.State.Trim(),
                ZipCode = request.ZipCode.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
            };

            _dbContext.UserAddresses.Add(address);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "Database unavailable while saving user address during registration.");
            await TryRollbackUserAsync(user);
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error while saving user address during registration.");
            await TryRollbackUserAsync(user);
            return HandlerResult<RegisterResponse>.Fail(
                StatusCodes.Status503ServiceUnavailable,
                "Registration is temporarily unavailable. Please try again in a moment.");
        }

        var response = new RegisterResponse(
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            user.ProfileImageBase64);
        return HandlerResult<RegisterResponse>.Ok(response);
    }

    private async Task TryRollbackUserAsync(ApplicationUser user)
    {
        try
        {
            await _userManager.DeleteAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback newly created user after address save failed.");
        }
    }

    private static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
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
