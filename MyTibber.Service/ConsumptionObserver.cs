using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using MyTibber.Common.Interfaces;
using MyTibber.Common.Repositories;
using MyTibber.Common.Services;
using Tibber.Sdk;

namespace MyTibber.Service;

public sealed class ConsumptionObserver(ILogger<ConsumptionObserver> logger, HeatpumpReposiory heaterReposiory, IEnergyRepository energyRepository) : IObserver<RealTimeMeasurement>
{
    private DateTime _lastEnergyPriceAdjustmentCheck = DateTime.MinValue;

    public void OnCompleted()
    {
        logger.LogInformation($"{nameof(ConsumptionObserver)} completed");
    }

    public void OnError(Exception error)
    {
        logger.LogError(error.Message);
    }

    public void OnNext(RealTimeMeasurement value)
    {
        logger.LogInformation($"Current power usage: {value.Power:N0} W (average: {value.AveragePower:N0} W); Consumption last hour: {value.AccumulatedConsumptionLastHour:N3} kWh; Cost since last midnight: {value.AccumulatedCost:N2} {value.Currency}");

        var minutesSinceLastCheck = (DateTime.Now - _lastEnergyPriceAdjustmentCheck).Minutes;
        if (minutesSinceLastCheck < 10)
        {
            logger.LogDebug($"Less then 10 minutes passed since last energy price check. Current time: {DateTime.Now}. Last check: {_lastEnergyPriceAdjustmentCheck}. Minutes since last check: {minutesSinceLastCheck}");
            return;
        }

        AdjustHeat(value.AccumulatedConsumptionLastHour).SafeFireAndForget(e => logger.LogError($"Adjust heat failed: {e.Message}"));
    }

    private async Task AdjustHeat(decimal accumulatedConsumptionLastHour)
    {
        var prices = await energyRepository.GetTodaysEnergyPrices();

        var priceAdjustment = HeatRegulator.CalculateHeatAdjustments(prices, DateTime.Now.Hour);
        var effectTaxAdjustment = CalculatEffectTaxAdjustment(accumulatedConsumptionLastHour);

        var heatAdjustment = priceAdjustment.Adjustment + effectTaxAdjustment;

        var currentHeat = await heaterReposiory.GetCurrentHeat();

        logger.LogDebug(
            $"Calculating if heat needs to be adjusted. " +
            $"Current heat: {currentHeat.RawValue}. Last update: {currentHeat.Timestamp}. " +
            $"Adjustment considering the effect tax: {effectTaxAdjustment}. " +
            $"Adjustment considering the energy price {priceAdjustment.Price} SEK ({priceAdjustment.Level}): {priceAdjustment.Adjustment}");

        if (heatAdjustment != currentHeat.RawValue)
        {
            logger.LogInformation("Setting the heat to {newHeat}", heatAdjustment);

            await heaterReposiory.UpdateHeat(heatAdjustment);
        }
        else
        {
            logger.LogDebug("No need to adjust the heat. Current heat: {CurrentHeat}. Last update: {HeatLatestUpdate}", currentHeat.Value, currentHeat.Timestamp);
        }

        _lastEnergyPriceAdjustmentCheck = DateTime.Now;
    }

    private static bool IsHourlyEffectLimitExceeded(decimal accumulatedConsumptionLastHour)
    {
        const decimal KWH_HOURLY_LIMIT = 2m;
        return accumulatedConsumptionLastHour >= KWH_HOURLY_LIMIT;
    }

    public static int CalculatEffectTaxAdjustment(decimal accumulatedConsumptionLastHour)
    {
        return IsHourlyEffectLimitExceeded(accumulatedConsumptionLastHour) ? -3 : 0;
    }
}

