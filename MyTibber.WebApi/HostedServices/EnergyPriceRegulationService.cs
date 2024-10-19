using Cronos;
using MyTibber.Common.Interfaces;
using MyTibber.Common.Models;
using MyTibber.Common.Repositories;
using MyTibber.Common.Services;

namespace MyTibber.WebApi.HostedServices;

public sealed class EnergyPriceRegulationService : BackgroundService
{
    private const string _schedule = "0 * * * *"; // every hour
    //private const string _schedule = "*/20 * * * * *";  // every 20 seconds (for testing)

    private readonly CronExpression _cron;
    private readonly IServiceProvider _services;
    private readonly ILogger<EnergyPriceRegulationService> _logger;

    public EnergyPriceRegulationService(IServiceProvider services, ILogger<EnergyPriceRegulationService> logger)
    {
        _services = services;
        _logger = logger;

        _cron = CronExpression.Parse(_schedule);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{nameof(EnergyPriceRegulationService)} is running.");

        await DoWorkAsync();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var nextUtc = _cron.GetNextOccurrence(utcNow) ?? throw new InvalidOperationException($"{nameof(_cron.GetNextOccurrence)} returned null");

                _logger.LogInformation($"Next occurrence (utc): {nextUtc}");

                await Task.Delay(nextUtc - utcNow, stoppingToken);

                await DoWorkAsync();
            }

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"{nameof(EnergyPriceRegulationService)} is stopping.");
        }
    }

    private async Task DoWorkAsync()
    {
        _logger.LogInformation($"{nameof(EnergyPriceRegulationService)} is working.");

        using var scope = _services.CreateScope();

        var energyRepository = scope.ServiceProvider.GetRequiredService<IEnergyRepository>();

        var prices = await energyRepository.GetTodaysEnergyPrices();

        var priceAdjustment = HeatRegulator.CalculateHeatAdjustments(prices, DateTime.Now.Hour);
        var newHeat = GetNewHeat(priceAdjustment);

        _logger.LogInformation($"{DateTime.Now} - Setting heat to {newHeat}. Current energy price {priceAdjustment.Price} SEK ({priceAdjustment.Level}). Price level considering todays prices: {priceAdjustment.DayPriceLevel}");

        var heaterReposiory = scope.ServiceProvider.GetRequiredService<HeaterReposiory>();
        //await heaterReposiory.UpdateHeat(priceAdjustment.Adjustment);
    }

    private static int GetNewHeat(HeatAdjustment priceAdjustment)
    {
        return priceAdjustment.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 0,
            DayPriceLevel.Low => 2,
            DayPriceLevel.High => -3,
            _ => 0,
        };
    }
}
