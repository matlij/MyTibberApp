using Cronos;
using MyTibber.Common;

namespace MyTibber.WebUi.HostedServices;

public sealed class HeatRegulatorHost : BackgroundService
{
    private readonly CronExpression _cron = CronExpression.Parse("0 * * * *"); // Every hour;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HeatRegulatorHost> _logger;

    public HeatRegulatorHost(
        IServiceScopeFactory scopeFactory,
        ILogger<HeatRegulatorHost> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Service} is running.", nameof(HeatRegulatorHost));

        try
        {
            await DoWorkAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var next = _cron.GetNextOccurrence(utcNow)
                    ?? throw new InvalidOperationException("Failed to calculate next occurrence time");

                var delay = next - utcNow;
                _logger.LogInformation("Next run: {NextTime} (in {Delay})",
                    next.ToLocalTime().ToString("HH:mm"),
                    delay.ToString(@"mm\:ss"));

                await Task.Delay(delay, stoppingToken);
                await DoWorkAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{Service} is stopping.", nameof(HeatRegulatorHost));
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is working.", nameof(HeatRegulatorHost));

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var heatResulatorService = scope.ServiceProvider.GetRequiredService<HeatResulatorService>();

            await heatResulatorService.RegulateHeat(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heat regulation cycle failed");
        }
    }
}
