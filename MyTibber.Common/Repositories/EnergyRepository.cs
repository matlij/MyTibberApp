using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.Common.Repositories;

public class StubbedEnergyRepository
{
    public Task<IEnumerable<Price>> GetTomorrowsEnergyPrices() =>
        Task.FromResult(GenerateStubPrices(DateTime.Today.AddDays(1)));

    public Task<IEnumerable<Price>> GetTodaysEnergyPrices() =>
        Task.FromResult(GenerateStubPrices(DateTime.Today));

    private IEnumerable<Price> GenerateStubPrices(DateTime date)
    {
        var prices = new List<Price>();
        var random = new Random(date.DayOfYear); // Use day of year as seed for consistent randomness

        for (int hour = 0; hour < 24; hour++)
        {
            var basePrice = 0.15m + (decimal)random.NextDouble() * 0.2m; // Random price between 0.15 and 0.35
            var startsAt = date.Date.AddHours(hour);

            prices.Add(new Price
            {
                Total = Math.Round(basePrice, 4),
                Energy = Math.Round(basePrice * 0.7m, 4),
                Tax = Math.Round(basePrice * 0.3m, 4),
                StartsAt = startsAt.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                Currency = "SEK",
                Level = GetPriceLevel(basePrice)
            });
        }

        return prices;
    }

    private PriceLevel GetPriceLevel(decimal price)
    {
        if (price < 0.2m) return PriceLevel.VeryExpensive;
        if (price < 0.25m) return PriceLevel.Cheap;
        if (price < 0.3m) return PriceLevel.Normal;
        if (price < 0.35m) return PriceLevel.Expensive;
        return PriceLevel.VeryExpensive;
    }
}

public class EnergyRepository
{
    private readonly TibberApiClient _tibberApiClient;

    public EnergyRepository(TibberApiClient tibberApiClient)
    {
        _tibberApiClient = tibberApiClient;
    }

    public Task<IEnumerable<Price>> GetTomorrowsEnergyPrices() =>
        GetEnergyPrices(PriceType.Tomorrow);

    public Task<IEnumerable<Price>> GetTodaysEnergyPrices() =>
        GetEnergyPrices(PriceType.Today);

    private async Task<IEnumerable<Price>> GetEnergyPrices(PriceType priceType)
    {
        var homeId = await GetHomeId();
        var customQuery = BuildCustomQuery(homeId, priceType);
        var result = await _tibberApiClient.Query(customQuery);

        return GetPricesFromResult(result, priceType) ??
            throw new TibberApiException(GetErrorMessage(result.Data, priceType));
    }

    private async Task<Guid> GetHomeId()
    {
        var basicData = await _tibberApiClient.GetBasicData();
        return basicData.Data.Viewer.Homes.First().Id.Value;
    }

    private string BuildCustomQuery(Guid homeId, PriceType priceType)
    {
        var priceInfoBuilder = new PriceInfoQueryBuilder()
            .WithToday(new PriceQueryBuilder().WithAllScalarFields())
            .WithTomorrow(new PriceQueryBuilder().WithAllScalarFields());

        var customQueryBuilder = new TibberQueryBuilder()
            .WithAllScalarFields()
            .WithViewer(
                new ViewerQueryBuilder()
                    .WithAllScalarFields()
                    .WithHome(
                        new HomeQueryBuilder()
                            .WithAllScalarFields()
                            .WithCurrentSubscription(
                                new SubscriptionQueryBuilder()
                                    .WithAllScalarFields()
                                    .WithPriceInfo(priceInfoBuilder)
                            ),
                        homeId
                    )
            );

        return customQueryBuilder.Build();
    }

    private IEnumerable<Price>? GetPricesFromResult(TibberApiQueryResponse result, PriceType priceType)
    {
        return priceType switch
        {
            PriceType.Today => result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Today,
            PriceType.Tomorrow => result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Tomorrow,
            _ => throw new ArgumentOutOfRangeException(nameof(priceType), priceType, null)
        };
    }

    private string GetErrorMessage(dynamic data, PriceType priceType)
    {
        return $"Error getting {priceType.ToString().ToLower()} energy prices. Some data in the response is missing:\n{JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })}";
    }

    private enum PriceType
    {
        Today,
        Tomorrow
    }
}
