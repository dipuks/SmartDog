using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartDog.Api.Middleware;
using SmartDog.Domain.Models;
using System.Text.Json;

namespace SmartDog.Tests;

public class ConcurrencyExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ConcurrencyException_ReturnsConflictProblemDetails()
    {
        var handler = new ConcurrencyExceptionHandler(
            NullLogger<ConcurrencyExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        context.RequestServices = services;
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new DbUpdateConcurrencyException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            "Concurrency conflict",
            document.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            StatusCodes.Status409Conflict,
            document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task TryHandleAsync_InvalidStatusTransition_ReturnsConflictProblemDetails()
    {
        var handler = new ConcurrencyExceptionHandler(
            NullLogger<ConcurrencyExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        context.RequestServices = services;
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new DecisionStatusTransitionException(
                DecisionStatuses.Completed,
                DecisionStatuses.InProgress),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            "Invalid decision status transition",
            document.RootElement.GetProperty("title").GetString());
    }
}