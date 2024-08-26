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

    public async Task<IEnumerable<Price>> GetTomorrowsEnergyPrices()
    {
        var basicData = await _tibberApiClient.GetBasicData();
        var homeId = basicData.Data.Viewer.Homes.First().Id.Value;

        var customQueryBuilder =
            new TibberQueryBuilder()
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
                                        .WithPriceInfo(new PriceInfoQueryBuilder()
                                            .WithTomorrow(new PriceQueryBuilder()
                                                .WithAllScalarFields()))
                                ),
                            homeId
                        )
                );

        var customQuery = customQueryBuilder.Build(); // produces plain GraphQL query text
        var result = await _tibberApiClient.Query(customQuery);

        return result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Tomorrow 
            ?? throw new TibberApiException($"Error getting today's energy prices. Some data in the response is missing:\n{JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true })}");
    }
}
