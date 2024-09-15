using Tibber.Sdk;

namespace MyTibber.CostPlanner
{
    public class HeatAdjustment
    {
        public DateTime Time { get; init; }
        public decimal Price { get; init; }
        public PriceLevel Level { get; init; }
        public int Adjustment { get; set; }
    }
}