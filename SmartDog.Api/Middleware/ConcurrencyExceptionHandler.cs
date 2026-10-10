using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartDog.Domain.Models;

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
        var isUniqueConstraintViolation =
            exception is DbUpdateException
            {
                InnerException: SqlException { Number: 2601 or 2627 }
            };

        if (exception is not DbUpdateConcurrencyException and
            not DecisionStatusTransitionException and
            not InvalidScoringConfigurationException &&
            !isUniqueConstraintViolation)
        {
            return false;
        }

        _logger.LogWarning(
            "A data conflict occurred for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        var (title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (
                "Concurrency conflict",
                "The decision was modified by another request. Retrieve the latest version and retry."),
            DbUpdateException => (
                "Data conflict",
                "The request conflicts with data that already exists. Refresh the resource and retry."),
            DecisionStatusTransitionException => (
                "Invalid decision status transition",
                "The requested status transition is not allowed."),
            _ => (
                "Scoring configuration is incomplete",
                "Criterion weights must total 100 and every alternative must be rated for each criterion.")
        };

        await Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: title,
            detail: detail)
            .ExecuteAsync(httpContext);

        return true;
    }
}