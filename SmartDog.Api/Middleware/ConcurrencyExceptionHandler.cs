using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SmartDog.Api.Middleware;

public sealed class ConcurrencyExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ConcurrencyExceptionHandler> _logger;

    public ConcurrencyExceptionHandler(
        ILogger<ConcurrencyExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        _logger.LogWarning(
            "A database concurrency conflict occurred for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Concurrency conflict",
            detail: "The decision was modified by another request. Retrieve the latest version and retry.")
            .ExecuteAsync(httpContext);

        return true;
    }
}