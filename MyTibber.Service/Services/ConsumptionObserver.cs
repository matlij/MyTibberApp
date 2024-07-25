using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace MyTibber.Service.Services;

public sealed class ConsumptionObserver : IObserver<RealTimeMeasurement>
{
    private readonly ILogger<ConsumptionObserver> _logger;
    private readonly HeaterService _heaterService;

    public ConsumptionObserver(ILogger<ConsumptionObserver> logger, HeaterService heaterService)
    {
        _logger = logger;
        _heaterService = heaterService;
    }

    public void OnCompleted()
    {
        _logger.LogInformation($"{nameof(ConsumptionObserver)} completed");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error.Message);
    }

    public void OnNext(RealTimeMeasurement value)
    {
        _logger.LogInformation($"power: {value.Power:N0} W (average: {value.AveragePower:N0} W); consumption last hour: {value.AccumulatedConsumptionLastHour:N3} kWh; cost since last midnight: {value.AccumulatedCost:N2} {value.Currency}");

        _heaterService.AdjustHeat(value.AccumulatedConsumptionLastHour).SafeFireAndForget(e => _logger.LogError($"Adjust heat failed: {e.Message}"));
    }
}

