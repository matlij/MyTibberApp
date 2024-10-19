using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        });

        var average = prices.Average(p => p.Total);

        var highThreshold = 2.5m * average;

        adjustments
            .Where(a => a.Price > highThreshold && (a.Level == PriceLevel.Expensive || a.Level == PriceLevel.VeryExpensive))
            .OrderByDescending(a => a.Price)
            .Take(4).ToList()
            .ForEach(a => a.DayPriceLevel = DayPriceLevel.High);

        var highAdjustments = adjustments.Count(a => a.DayPriceLevel == DayPriceLevel.High);

        adjustments
            .Where(a => a.Price > average && (a.Level == PriceLevel.Cheap || a.Level == PriceLevel.VeryCheap))
            .OrderBy(a => a.Price)
            .Take(highAdjustments).ToList()
            .ForEach(a => a.DayPriceLevel = DayPriceLevel.Low);

        return adjustments;
    }
}