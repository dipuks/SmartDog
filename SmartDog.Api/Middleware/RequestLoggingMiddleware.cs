namespace SmartDog.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation(
            "Incoming request {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "Request {Method} {Path} completed with status {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Request {Method} {Path} failed after {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}