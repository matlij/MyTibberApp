using MyTibber.Common.Interfaces;
using System.Collections.ObjectModel;
using Tibber.Sdk;

namespace MyTibber.Common.Repositories;

public class EnergyRepositoryStubbed : IEnergyRepository
{
    public Task<ReadOnlyCollection<Price>> GetTomorrowsEnergyPrices() =>
        Task.FromResult(GenerateStubPrices(DateTime.Today.AddDays(1)));

    public Task<ReadOnlyCollection<Price>> GetTodaysEnergyPrices() =>
        Task.FromResult(GenerateStubPrices(DateTime.Today));

    private ReadOnlyCollection<Price> GenerateStubPrices(DateTime date)
    {
        var prices = new List<Price>();
        var random = new Random(date.DayOfYear); // Use day of year as seed for consistent randomness

        for (int hour = 0; hour < 24; hour++)
        {
            var basePrice = 0.15m + (decimal)random.NextDouble() * 0.2m; // Random price between 0.15 and 0.35
            var startsAt = date.Date.AddHours(hour);

            prices.Add(new Price
            {
                Total = Math.Round(basePrice, 4),
                Energy = Math.Round(basePrice * 0.7m, 4),
                Tax = Math.Round(basePrice * 0.3m, 4),
                StartsAt = startsAt.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                Currency = "SEK",
                Level = GetPriceLevel(basePrice)
            });
        }

        return new ReadOnlyCollection<Price>(prices);
    }

    private PriceLevel GetPriceLevel(decimal price)
    {
        if (price < 0.2m) return PriceLevel.VeryExpensive;
        if (price < 0.25m) return PriceLevel.Cheap;
        if (price < 0.3m) return PriceLevel.Normal;
        if (price < 0.35m) return PriceLevel.Expensive;
        return PriceLevel.VeryExpensive;
    }
}
