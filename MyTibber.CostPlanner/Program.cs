using MyTibber.Common.Repositories;
using System.Net.Http.Headers;
using Tibber.Sdk;

namespace MyTibber.CostPlanner
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var userAgent = new ProductInfoHeaderValue("My-home-automation-system", "1.2");
            //var tibbeApiClient = new TibberApiClient("hHYECYJUfCcxUbfFasjYmi4t59TDLFPPkE2Ox9yL214", userAgent);
            //var repository = new EnergyRepository(tibbeApiClient);
            //var prices = await repository.GetTodaysEnergyPrices();

            //var adjustments = HeatAdjustmentCalculator.CalculateHeatAdjustments(prices.ToList());

            //foreach (var adjustment in adjustments)
            //{
            //    Console.WriteLine($"{adjustment.Time} {adjustment.Price} SEK ({adjustment.Level}): {adjustment.Adjustment}");
            //}
        }
    }
}