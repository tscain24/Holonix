using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Handlers.Auth;

public sealed class RegisterUserHandler : IRegisterUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IQueryableUserStore<ApplicationUser> _userStore;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        UserManager<ApplicationUser> userManager,
        IQueryableUserStore<ApplicationUser> userStore,
        ILogger<RegisterUserHandler> logger)
    {
        _userManager = userManager;
        _userStore = userStore;
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
            existingUser = await _userStore.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    user => user.NormalizedEmail == normalizedEmail && user.InactiveDate == null,
                    cancellationToken);
        }
        catch (SqlException ex)
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
            DateOfBirth = dateOfBirth,
            ProfileImageBase64 = NormalizeBase64Image(request.ProfileImageBase64),
        };

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, request.Password);
        }
        catch (SqlException ex)
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

        var response = new RegisterResponse(
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            user.ProfileImageBase64);
        return HandlerResult<RegisterResponse>.Ok(response);
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
