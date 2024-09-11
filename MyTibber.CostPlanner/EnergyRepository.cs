using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.Service.Services;

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
