using MyTibber.Common.Models;
using Tibber.Sdk;

namespace MyTibber.Common.Services;

public class HeatRegulator
{
    public static EnergyPrice CreateEneryPrices(ICollection<Price> prices, int hour)
    {
        if (hour < 0 || hour > 24)
        {
            throw new ArgumentException($"{nameof(hour)} must be between 0 and 24");
        }

        return CreateEneryPrices(prices).FirstOrDefault(h => h.Time.Hour == hour)
            ?? throw new InvalidOperationException($"Hour {hour} not found");
    }

    public static IEnumerable<EnergyPrice> CreateEneryPrices(ICollection<Price> priceDtos)
    {
        ValidatePrices(priceDtos);

        var prices = priceDtos
            .OrderBy(p => p.StartsAt)
            .Select(p => new EnergyPrice
            {
                Time = DateTime.Parse(p.StartsAt),
                Price = p.Total ?? 0,
                Level = p.Level ?? PriceLevel.Normal,
                DayPriceLevel = DayPriceLevel.Normal,
            }).ToList();

        var average = priceDtos.Average(p => p.Total) ?? 0;
        if (average == 0)
        {
            return prices;
        }

        var veryHighThreshold = 1.6m * average;
        var highThreshold = 1.3m * average;
        var lowThreshold = 0.7m * average;
        var veryLowThreshold = 0.4m * average;

        foreach (var price in prices)
        {
            price.DayPriceLevel = GetDayPriceLevel(veryHighThreshold, highThreshold, lowThreshold, veryLowThreshold, price);
        }

        return prices;
    }

    private static DayPriceLevel GetDayPriceLevel(decimal veryHighThreshold, decimal highThreshold, decimal lowThreshold, decimal veryLowThreshold, EnergyPrice? price)
    {
        if (price is null)
        {
            return DayPriceLevel.Normal;
        }

        if (price.Price >= veryHighThreshold && price.Level == PriceLevel.VeryExpensive)
        {
            return DayPriceLevel.VeryHigh;
        }
        else if (price.Price >= highThreshold && (price.Level == PriceLevel.Expensive || price.Level == PriceLevel.VeryExpensive))
        {
            return DayPriceLevel.High;
        }
        else if (price.Price <= veryLowThreshold && price.Level == PriceLevel.VeryCheap)
        {
            return DayPriceLevel.VeryLow;
        }
        else if (price.Price <= lowThreshold && (price.Level == PriceLevel.Cheap || price.Level == PriceLevel.VeryCheap))
        {
            return DayPriceLevel.Low;
        }

        return DayPriceLevel.Normal;
    }

    private static void ValidatePrices(ICollection<Price> prices)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(prices));

        if (prices.Count > 25 || prices.Count < 23) // Due to day light savings some days will have 25 or 23 hours
        {
            throw new ArgumentException($"Too many or too few prices in the {nameof(prices)} parameter: {prices.Count}. List of prices: {GetDatesAsString(prices)}");
        }

        for (int i = 0; i < 24; i++)
        {
            if (!prices.Any(p => DateTime.Parse(p.StartsAt).Hour == i))
            {
                throw new ArgumentException($"Missing hour {i} in list of prices. List of prices: {GetDatesAsString(prices)}");
            }
        }

        static string GetDatesAsString(ICollection<Price> prices)
        {
            return string.Join(',', prices.Select(p => p.StartsAt));
        }
    }
}