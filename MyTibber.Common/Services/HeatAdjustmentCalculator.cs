using MyTibber.Common.Interfaces;
using MyTibber.Common.Models;
using MyTibber.Common.Repositories;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Tibber.Sdk;

namespace MyTibber.Common.Services
{
    public class HeatAdjustmentCalculator
    {
        public static HeatAdjustment CalculateEnergyPriceAdjustment(ReadOnlyCollection<Price> prices, int hour)
        {
            if (hour < 0 || hour > 24)
                throw new ArgumentException($"Value of hour ({hour}) must be between 0 and 24.");

            var adjustments = CalculateEnergyPriceAdjustments(prices);

            return adjustments.FirstOrDefault(a => a.Time.TimeOfDay.Hours == hour)
                ?? throw new ArgumentException($"Couldn't find price adjustment for hour: {hour}");
        }

        public static IEnumerable<HeatAdjustment> CalculateEnergyPriceAdjustments(ReadOnlyCollection<Price> prices)
        {
            var adjustments = new List<HeatAdjustment>();

            for (int i = 0; i < prices.Count; i++)
            {
                var price = prices[i];
                var baseAdjustment = CalculateBaseAdjustment(price.Level ?? PriceLevel.Normal);
                var lookAheadModifier = GetLookAheadModifier(prices, i);

                var finalAdjustment = baseAdjustment + lookAheadModifier;
                finalAdjustment = Math.Clamp(finalAdjustment, -2, 2);  // Ensure adjustment is between -2 and 2

                adjustments.Add(new HeatAdjustment
                {
                    Time = DateTime.Parse(price.StartsAt),
                    Price = price.Total ?? 0,
                    Level = price.Level ?? PriceLevel.Normal,
                    Adjustment = finalAdjustment
                });
            }

            return SmoothAdjustments(adjustments);
        }

        private static int CalculateBaseAdjustment(PriceLevel level) => level switch
        {
            PriceLevel.VeryExpensive => -2,
            PriceLevel.Expensive => -1,
            PriceLevel.Normal => 0,
            PriceLevel.Cheap => 1,
            PriceLevel.VeryCheap => 2,
            _ => 0
        };

        private static int GetLookAheadModifier(ReadOnlyCollection<Price> prices, int currentIndex)
        {
            var lookAheadHours = 2;
            if (currentIndex + lookAheadHours >= prices.Count) return 0;

            var futurePrice = prices[currentIndex + lookAheadHours];
            if (futurePrice.Level == PriceLevel.Expensive || futurePrice.Level == PriceLevel.VeryExpensive)
            {
                return 1;  // Increase heat before a high-price period
            }
            return 0;
        }

        private static List<HeatAdjustment> SmoothAdjustments(List<HeatAdjustment> adjustments)
        {
            var smoothedAdjustments = new List<HeatAdjustment>(adjustments);
            for (int i = 1; i < smoothedAdjustments.Count - 1; i++)
            {
                var prev = smoothedAdjustments[i - 1].Adjustment;
                var current = smoothedAdjustments[i].Adjustment;
                var next = smoothedAdjustments[i + 1].Adjustment;

                // If current adjustment is different from both neighbors, smooth it
                if (current > prev && current > next || current < prev && current < next)
                {
                    smoothedAdjustments[i].Adjustment = (prev + next) / 2;
                }
            }
            return smoothedAdjustments;
        }
    }
}