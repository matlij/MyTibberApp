using Tibber.Sdk;

namespace MyTibber.Service.Services;

public class EnergyRepository
{
    private readonly TibberApiClient _tibberApiClient;

    public EnergyRepository(TibberApiClient tibberApiClient)
    {
        _tibberApiClient = tibberApiClient;
    }

    public async Task<IEnumerable<Price>> GetTodaysEneryPrices()
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
                                            .WithToday(new PriceQueryBuilder()
                                                .WithAllScalarFields()))
                                ),
                            homeId
                        )
                );

        var customQuery = customQueryBuilder.Build(); // produces plain GraphQL query text
        var result = await _tibberApiClient.Query(customQuery);

        return result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Today 
            ?? throw new TibberApiException("Error getting today's energy prices");
    }
}
