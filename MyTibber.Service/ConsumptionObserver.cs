using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace MyTibber.Service;

public sealed class ConsumptionObserver : IObserver<RealTimeMeasurement>
{
    private readonly ILogger<ConsumptionObserver> _logger;

    public ConsumptionObserver(ILogger<ConsumptionObserver> logger) 
    {
        _logger = logger;
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
    }
}
