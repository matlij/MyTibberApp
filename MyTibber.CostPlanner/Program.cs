using MyTibber.Service.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.CostPlanner
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var userAgent = new ProductInfoHeaderValue("My-home-automation-system", "1.2");
            var tibbeApiClient = new TibberApiClient("hHYECYJUfCcxUbfFasjYmi4t59TDLFPPkE2Ox9yL214", userAgent);
            var repository = new EnergyRepository(tibbeApiClient);

            var prices = await repository.GetTodaysEnergyPrices();
            
            var priceStats = CalculatePriceStatistics(prices);

            Console.WriteLine(JsonSerializer.Serialize(priceStats, new JsonSerializerOptions { WriteIndented = true }));

            foreach (var price in prices) 
            {
                var heatAdjustment = CalculateSettingAdjustment(price.Total ?? 0, priceStats);

                Console.WriteLine($"{price.StartsAt} {price.Total} {price.Currency}: {heatAdjustment}");
            }
        }

        static PriceStatistics CalculatePriceStatistics(IEnumerable<Price> prices)
        {
            var priceValues = prices
                .Where(p => p.Total != null)
                .Select(p => p.Total ?? 0)
                .ToList() ?? [];

            return new PriceStatistics
            {
                MinPrice = priceValues.Min(),
                MaxPrice = priceValues.Max(),
                AveragePrice = priceValues.Average()
            };
        }

        static int CalculateSettingAdjustment(decimal price, PriceStatistics priceStats)
        {
            if (price <= priceStats.LowThreshold) return 1;       // Very low price: increase setting by 2
            else if (price < priceStats.AveragePrice) return 0;  // Below average price: increase setting by 1
            else if (price < priceStats.HighThreshold) return 0;  // Average price: no change
            else if (price < priceStats.MaxPrice) return -1; // High price: decrease setting by 1
            else return -2;                    // Very high price: decrease setting by 2
        }
    }

    class PriceStatistics
    {
        public decimal MinPrice { get; init; }
        public decimal MaxPrice { get; init; }
        public decimal AveragePrice { get; init; }
        public decimal Range => MaxPrice - MinPrice;
        public decimal LowThreshold => MinPrice + (Range * 0.33m);
        public decimal HighThreshold => MaxPrice - (Range * 0.33m);

    }
}
