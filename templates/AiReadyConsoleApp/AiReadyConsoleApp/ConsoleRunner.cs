using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AiReadyConsoleApp;

public class ConsoleRunner : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ConsoleRunner> _logger;

    public ConsoleRunner(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<ConsoleRunner> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return Task.CompletedTask;
        }
        catch (Exception ex)
        when (HandleWithoutLosingLoggingScope(() => _logger.LogCritical(ex, "An unexpected fatal error has occurred.")))
        {
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation("AiReadyConsoleApp completed in {ElapsedTime}", stopwatch.Elapsed);
            _hostApplicationLifetime.StopApplication();
        }
    }

    private bool HandleWithoutLosingLoggingScope(Action action)
    {
        action();
        return false;
    }
}
