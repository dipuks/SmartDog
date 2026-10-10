namespace SmartDog.Api.Workers;

public class DecisionWorker : BackgroundService
{
    private readonly ILogger<DecisionWorker> _logger;

    public DecisionWorker(ILogger<DecisionWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Decision worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Decision worker is running at {Time}",
                    DateTime.UtcNow);

                await Task.Delay(
                    TimeSpan.FromSeconds(30),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the application is shutting down.
                break;
            }
        }

        _logger.LogInformation("Decision worker stopped.");
    }
}