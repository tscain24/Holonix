using Microsoft.AspNetCore.Http;

namespace Holonix.Server.Application.Results;

public sealed record HandlerResult<T>(bool Success, int StatusCode, T? Payload, string[] Errors)
{
    public static HandlerResult<T> Ok(T payload)
        => new(true, StatusCodes.Status200OK, payload, Array.Empty<string>());

    public static HandlerResult<T> Fail(int statusCode, params string[] errors)
        => new(false, statusCode, default, errors);
}

