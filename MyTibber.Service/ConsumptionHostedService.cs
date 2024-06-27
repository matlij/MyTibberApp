using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibber.Service;

public sealed class ConsumptionHostedService : IHostedService
{
    private readonly ILogger _logger;
    private readonly string _accessToken = "hHYECYJUfCcxUbfFasjYmi4t59TDLFPPkE2Ox9yL214";
    private readonly TibberApiClient _client;
    private readonly IObserver<RealTimeMeasurement> _observer;

    public ConsumptionHostedService(
        ILogger<ConsumptionHostedService> logger,
        IObserver<RealTimeMeasurement> observer)
    {
        _logger = logger;
        _observer = observer;

        var userAgent = new ProductInfoHeaderValue("My-home-automation-system", "1.2");
        _client = new TibberApiClient(_accessToken, userAgent);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartAsync has been called.");

        var homeId = await GetHomeId(_client, cancellationToken);

        var listener = await _client.StartRealTimeMeasurementListener(homeId, cancellationToken);
        listener.Subscribe(_observer);

        _logger.LogInformation("Real Time Measurement listener started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync has been called.");

        var homeId = await GetHomeId(_client, cancellationToken);

        await _client.StopRealTimeMeasurementListener(homeId);

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