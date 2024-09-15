using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using MyTibber.Common.Interfaces;
using MyTibber.Common.Repositories;
using MyTibber.Common.Services;
using Tibber.Sdk;

namespace MyTibber.Service;

public sealed class ConsumptionObserver(ILogger<ConsumptionObserver> logger, HeaterReposiory heaterReposiory, IEnergyRepository energyRepository) : IObserver<RealTimeMeasurement>
{
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

        AdjustHeat(value.AccumulatedConsumptionLastHour).SafeFireAndForget(e => logger.LogError($"Adjust heat failed: {e.Message}"));
    }

    private async Task AdjustHeat(decimal accumulatedConsumptionLastHour)
    {
        var prices = await energyRepository.GetTodaysEnergyPrices();

        var priceAdjustment = HeatAdjustmentCalculator.CalculateEnergyPriceAdjustment(prices, DateTime.Now.Hour);
        var effectTaxAdjustment = CalculatEffectTaxAdjustment(accumulatedConsumptionLastHour);

        var heatAdjustment = priceAdjustment.Adjustment + effectTaxAdjustment;

        var currentHeat = await heaterReposiory.GetCurrentHeat();

        if (currentHeat.IsPending)
        {
            logger.LogInformation("Current heat is pending. Aborting heat adjustment.");
            return;
        }


        logger.LogDebug(
            $"Calculating if heat needs to be adjusted. " +
            $"Current heat: {currentHeat.Value}. Last update: {currentHeat.Timestamp}. " +
            $"Adjustment considering the effect tax: {effectTaxAdjustment}. " +
            $"Adjustment considering the energy price {priceAdjustment.Price} SEK ({priceAdjustment.Level}): {priceAdjustment.Adjustment}");

        if (heatAdjustment != currentHeat.RawValue)
        {
            logger.LogDebug("Setting the heat to {newHeat}", heatAdjustment);

            await heaterReposiory.UpdateHeat(currentHeat.RawValue);
        }
        else
        {
            logger.LogDebug("No need to adjust the heat. Current heat: {CurrentHeat}. Last update: {HeatLatestUpdate}", currentHeat.Value, currentHeat.Timestamp);
        }

    }

    public static int CalculatEffectTaxAdjustment(decimal accumulatedConsumptionLastHour)
    {
        const decimal KWH_HOURLY_LIMIT = 2m;

        if (accumulatedConsumptionLastHour >= KWH_HOURLY_LIMIT)
        {
            return -3;
        }

        return 0;
    }
}

