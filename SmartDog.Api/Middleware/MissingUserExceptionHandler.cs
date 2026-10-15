using Microsoft.AspNetCore.Diagnostics;

namespace SmartDog.Api.Middleware;

/// <summary>
/// An authenticated request without a usable user id cannot be scoped to an owner, so it is refused with 401 instead of
/// reaching any decision data.
/// </summary>
public sealed class MissingUserExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedAccessException) return false;

        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Sign-in required",
            detail: "The access token does not identify a user. Sign in again.")
            .ExecuteAsync(httpContext);
        return true;
    }
}
