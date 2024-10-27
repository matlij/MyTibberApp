using MyTibber.Common.Models;

namespace MyTibber.Common.Utilities;

public class HeatCalculator
{
    public static int GetNewHeat(HeatAdjustment priceAdjustment)
    {
        return priceAdjustment.DayPriceLevel switch
        {
            DayPriceLevel.Normal => 0,
            DayPriceLevel.Low => 1,
            DayPriceLevel.High => -3,
            _ => 0,
        };
    }
}
