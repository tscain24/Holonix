using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Application.Results;
using Holonix.Server.Contracts.Auth;
using Holonix.Server.Domain.Entities;

namespace Holonix.Server.Application.Handlers.Auth;

public sealed class RegisterUserHandler : IRegisterUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterUserHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status409Conflict, "Email already in use.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = dateOfBirth,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return HandlerResult<RegisterResponse>.Fail(StatusCodes.Status400BadRequest, errors);
        }

        var response = new RegisterResponse(user.FirstName, user.LastName, user.Email ?? string.Empty);
        return HandlerResult<RegisterResponse>.Ok(response);
    }
}

