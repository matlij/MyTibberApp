using MyTibber.Common.Models;
using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.Common.Services;

public class HeatRegulator
{
    public static HeatAdjustment CalculateHeatAdjustments(ICollection<Price> prices, int hour)
    {
        if (hour < 0 || hour > 24)
        {
            throw new ArgumentException($"{nameof(hour)} must be between 0 and 24");
        }

        return CalculateHeatAdjustments(prices).FirstOrDefault(h => h.Time.Hour == hour)
            ?? throw new InvalidOperationException($"Hour {hour} not found");
    }

    public static IEnumerable<HeatAdjustment> CalculateHeatAdjustments(ICollection<Price> prices)
    {
        ValidatePrices(prices);

        var result = prices
            .OrderBy(p => p.StartsAt)
            .Select(p => new HeatAdjustment
            {
                Time = DateTime.Parse(p.StartsAt),
                Price = p.Total ?? 0,
                Level = p.Level ?? PriceLevel.Normal,
            }).ToList();

        var average = prices.Average(p => p.Total);
        var highThreshold = 2m * average;
        var lowThreshold = 0.5m * average;

        foreach (var a in result)
        {
            if (a.Price > highThreshold && (a.Level == PriceLevel.Expensive || a.Level == PriceLevel.VeryExpensive))
            {
                a.DayPriceLevel = DayPriceLevel.High;
            }

            else if (a.Price < lowThreshold && (a.Level == PriceLevel.Cheap || a.Level == PriceLevel.VeryCheap))
            {
                a.DayPriceLevel = DayPriceLevel.Low;
            }
        }

        return result;
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