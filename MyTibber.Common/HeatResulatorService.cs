using Microsoft.Extensions.Logging;
using MyTibber.Common.Interfaces;
using MyTibber.Common.Repositories;
using MyTibber.Common.Services;
using MyTibber.Common.Utilities;

namespace MyTibber.Common;

public class HeatResulatorService(
    IEnergyRepository energyRepository,
    HeatpumpClient heatpumpReposiory,
    WifiSocketsService wifiSocketsService,
    ILogger<HeatResulatorService> logger)
{
    private readonly IEnergyRepository _energyRepository = energyRepository ?? throw new ArgumentNullException(nameof(energyRepository));
    private readonly HeatpumpClient _heatpumpReposiory = heatpumpReposiory ?? throw new ArgumentNullException(nameof(heatpumpReposiory));
    private readonly WifiSocketsService _wifiSocketsService = wifiSocketsService;
    private readonly ILogger<HeatResulatorService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task RegulateHeat(CancellationToken cancellationToken = default)
    {
        var prices = await _energyRepository.GetTodaysEnergyPrices();
        if (prices.Count == 0)
        {
            _logger.LogWarning("No energy prices available for today");
            return;
        }

        var currentHour = DateTime.Now.Hour;
        var price = HeatRegulator.CalculateHeatAdjustments(prices, currentHour);
        var heatOffset = price.CalculateHeatOffset();
        var targetTemprature = price.CalculateTargetTemperature();

        _logger.LogInformation(
            "Current energy price {Price:F2} SEK ({Level}). " +
            "Price level considering today's prices: {DayPriceLevel}" +
            "Heat offset {HeatOffset}." +
            "target temprature {TargetTemprature}.",
            price.Price, price.Level, price.DayPriceLevel, heatOffset, targetTemprature);

        await _wifiSocketsService.UpdateAllClients(targetTemprature, cancellationToken);
        await _heatpumpReposiory.UpdateHeat(heatOffset, cancellationToken);
    }
}