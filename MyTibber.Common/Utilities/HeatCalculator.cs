using MyTibber.Common.Models;

namespace MyTibber.Common.Utilities;

public static class EnergyPriceExtensions
{
    public static int CalculateHeatOffset(this EnergyPrice price)
    {
        return price.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 0,
            DayPriceLevel.VeryLow => 2,
            DayPriceLevel.Low => 1,
            DayPriceLevel.High => -1,
            DayPriceLevel.VeryHigh => -2,
            _ => 0,
        };
    }

    public static ComfortMode CalculateComfortMode(this EnergyPrice price)
    {
        return price.DayPriceLevel switch
        {
            DayPriceLevel.Normal => ComfortMode.Economy,
            DayPriceLevel.VeryLow => ComfortMode.Luxury,
            DayPriceLevel.Low => ComfortMode.Normal,
            DayPriceLevel.High => ComfortMode.Economy,
            DayPriceLevel.VeryHigh => ComfortMode.Economy,
            _ => 0,
        };
    }

    public static int CalculateTargetTemperature(this EnergyPrice price)
    {
        return price.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 7,
            DayPriceLevel.VeryLow => 12,
            DayPriceLevel.Low => 10,
            DayPriceLevel.High => 5,
            DayPriceLevel.VeryHigh => 5,
            _ => 7,
        };
    }
}
