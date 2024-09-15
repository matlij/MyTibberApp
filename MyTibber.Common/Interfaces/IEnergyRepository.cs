using System.Collections.ObjectModel;
using Tibber.Sdk;

namespace MyTibber.Common.Interfaces
{
    public interface IEnergyRepository
    {
        Task<ReadOnlyCollection<Price>> GetTodaysEnergyPrices();
        Task<ReadOnlyCollection<Price>> GetTomorrowsEnergyPrices();
    }
}