using Microsoft.Extensions.Caching.Memory;
using MyTibber.Common.Interfaces;
using System.Collections.ObjectModel;
using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.Common.Repositories;

public class EnergyRepository(TibberApiClient tibberApiClient, IMemoryCache cache) : IEnergyRepository
{
    public Task<ReadOnlyCollection<Price>> GetTomorrowsEnergyPrices() =>
        GetEnergyPrices(PriceType.Tomorrow);

    public Task<ReadOnlyCollection<Price>> GetTodaysEnergyPrices() =>
        GetEnergyPrices(PriceType.Today);

    private async Task<ReadOnlyCollection<Price>> GetEnergyPrices(PriceType priceType)
    {
        var cacheKey = GetCacheKey(priceType);

        var result = await cache.GetOrCreateAsync(cacheKey, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpiration = new DateTimeOffset(cacheKey, TimeOnly.MaxValue, TimeSpan.Zero);

            var homeId = await GetHomeId();
            var query = BuildEnergyPricesQuery(homeId);
            var result = await tibberApiClient.Query(query);

            return GetPricesFromResult(result, priceType) ??
                throw new TibberApiException(GetTibberApiErrorMessage(result.Data));
        });

        return result ?? ReadOnlyCollection<Price>.Empty;
    }

    private async Task<Guid> GetHomeId()
    {
        var basicData = await tibberApiClient.GetBasicData();

        return basicData.Data.Viewer.Homes.FirstOrDefault()?.Id.GetValueOrDefault() ??
            throw new TibberApiException(GetTibberApiErrorMessage(basicData.Data));
    }

    private static string BuildEnergyPricesQuery(Guid homeId)
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

    private static DateOnly GetCacheKey(PriceType priceType)
    {
        return priceType switch
        {
            PriceType.Today => DateOnly.FromDateTime(DateTime.Now),
            PriceType.Tomorrow => DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(priceType), priceType, null),
        };
    }

    private static ReadOnlyCollection<Price>? GetPricesFromResult(TibberApiQueryResponse result, PriceType priceType)
    {
        var prices = priceType switch
        {
            PriceType.Today => result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Today,
            PriceType.Tomorrow => result.Data.Viewer.Home?.CurrentSubscription?.PriceInfo?.Tomorrow,
            _ => throw new ArgumentOutOfRangeException(nameof(priceType), priceType, null)
        };

        return prices != null ? new ReadOnlyCollection<Price>(prices.ToList()) : null;
    }

    private static string GetTibberApiErrorMessage(object data) => $"Get data from Tibber failed. Some data in the response is missing:\n{JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })}";

    private enum PriceType
    {
        Today,
        Tomorrow
    }
}
