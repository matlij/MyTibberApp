using System.Runtime.Serialization;
using Tibber.Sdk;

namespace MyTibber.Common.Models
{
    public enum DayPriceLevel
    {
        Normal,
        Low,
        High
    }

    public class EnergyPrice
    {
        public DateTime Time { get; init; }
        public decimal Price { get; init; }
        public PriceLevel Level { get; init; }
        public DayPriceLevel DayPriceLevel { get; set; } = DayPriceLevel.Normal;
        public int Adjustment { get; set; } // Remove
    }
}