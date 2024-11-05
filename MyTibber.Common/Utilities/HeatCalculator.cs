using MyTibber.Common.Models;

namespace MyTibber.Common.Utilities;

public static class EnergyPriceExtensions
{
    public static int CalculateHeatOffset(this EnergyPrice price)
    {
        return price.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 0,
            DayPriceLevel.Low => 1,
            DayPriceLevel.High => -3,
            _ => 0,
        };
    }

    public static int CalculateTargetTemperature(this EnergyPrice price)
    {
        return price.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 8,
            DayPriceLevel.Low => 13,
            DayPriceLevel.High => 5,
            _ => 0,
        };
    }
}
