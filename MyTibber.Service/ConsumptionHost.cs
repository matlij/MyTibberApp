using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace MyTibber.Service;

public sealed class ConsumptionHost(
    ILogger<ConsumptionHost> logger,
    IObserver<RealTimeMeasurement> observer,
    TibberApiClient tibberApiClient) : IHostedService
{
    private readonly ILogger _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(StartAsync)} has been called.");

        var validateRealtimeDeviceResult = await tibberApiClient.ValidateRealtimeDevice(cancellationToken);

        var homeId = await GetHomeId(tibberApiClient, cancellationToken);

        var listener = await tibberApiClient.StartRealTimeMeasurementListener(homeId, cancellationToken);
        listener.Subscribe(observer);

        _logger.LogInformation("Real Time Measurement listener started");

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync has been called.");

        var homeId = await GetHomeId(tibberApiClient, cancellationToken);

        await tibberApiClient.StopRealTimeMeasurementListener(homeId);

        _logger.LogInformation("Real Time Measurement listener stopped");
    }

    private static async Task<Guid> GetHomeId(TibberApiClient tibberApiClient, CancellationToken cancellationToken)
    {
        var basicData = await tibberApiClient.GetBasicData(cancellationToken);
        var home = basicData.Data.Viewer.Homes.First();
        if (home is null || home.Id is null)
        {
            throw new FormatException("Home or home ID returned from Tibber is null");
        }

        return home.Id.Value;
    }
}