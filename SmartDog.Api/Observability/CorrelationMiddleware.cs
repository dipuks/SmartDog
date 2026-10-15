using System.Text.RegularExpressions;

namespace SmartDog.Api.Observability;

/// <summary>Accepts a well-formed X-Correlation-ID (or creates one), echoes it, and adds it to the logging scope.</summary>
public sealed partial class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    [GeneratedRegex("^[A-Za-z0-9._-]{8,64}$")]
    private static partial Regex Valid();

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = Valid().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        // A message-template scope: structured providers get the CorrelationId property and the simple console
        // formatter (with IncludeScopes) prints it readably, unlike a dictionary scope.
        using (logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            await next(context);
        }
    }
}
