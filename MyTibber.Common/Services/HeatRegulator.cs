using MyTibber.Common.Models;
using Tibber.Sdk;

namespace MyTibber.Common.Services;

public class HeatRegulator
{
    public static HeatAdjustment CalculateHeatAdjustments(IEnumerable<Price> prices, int hour)
    {
        if (hour < 0 || hour > 24)
        {
            throw new ArgumentException($"{nameof(hour)} must be between 0 and 24");
        }

        return CalculateHeatAdjustments(prices).SingleOrDefault(h => h.Time.Hour == hour)
            ?? throw new InvalidOperationException($"Hour {hour} not found");
    }

    public static IEnumerable<HeatAdjustment> CalculateHeatAdjustments(IEnumerable<Price> prices)
    {
        var adjustments = prices.Select(p => new HeatAdjustment
        {
            Time = DateTime.Parse(p.StartsAt),
            Price = p.Total ?? 0,
            Level = p.Level ?? PriceLevel.Normal,
        }).ToList();

        var average = prices.Average(p => p.Total);
        var highThreshold = 2m * average;
        var lowThreshold = 0.5m * average;

        foreach (var a in adjustments)
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

        return adjustments;
    }
}