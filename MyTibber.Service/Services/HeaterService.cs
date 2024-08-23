using Microsoft.Extensions.Logging;
using Tibber.Sdk;

namespace MyTibber.Service.Services;

public class HeaterService
{
    private readonly ILogger<HeaterService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HeaterReposiory _heaterReposiory;
    private readonly TibberApiClient _tibberApiClient;

    public HeaterService(ILogger<HeaterService> logger, IHttpClientFactory httpClientFactory, HeaterReposiory heaterReposiory, TibberApiClient tibberApiClient)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _heaterReposiory = heaterReposiory;
        _tibberApiClient = tibberApiClient;
    }

    public async Task AdjustHeat(decimal accumulatedConsumptionLastHour)
    {
        const decimal HOURLY_POWER_LIMIT = 1m;

        var heat = await _heaterReposiory.GetCurrentHeat();

        var currentHeatIsBelowZero = heat.Value < 0;

        if (accumulatedConsumptionLastHour >= HOURLY_POWER_LIMIT && !currentHeatIsBelowZero)
        {
            await _heaterReposiory.UpdateHeat(-2);
        }
        else if (accumulatedConsumptionLastHour < HOURLY_POWER_LIMIT && currentHeatIsBelowZero)
        {
            await _heaterReposiory.UpdateHeat(0);
        }

        _logger.LogDebug("No need to adjust the heat. Current heat: {CurrentHeat}. Last update: {HeatLatestUpdate}", heat.Value, heat.Timestamp);
    }
}


