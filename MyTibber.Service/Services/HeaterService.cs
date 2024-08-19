using Microsoft.Extensions.Logging;
using MyTibber.Service.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Tibber.Sdk;

namespace MyTibber.Service.Services;

public class HeaterService
{
    private readonly ILogger<HeaterService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HeaterReposiory _heaterReposiory;
    private readonly TibberApiClient _tibberApiClient;

    public HeaterService(ILogger<HeaterService> logger, IHttpClientFactory httpClientFactory, HeaterReposiory heaterReposiory, TibberApiClient tibberApiClient)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _heaterReposiory = heaterReposiory;
        _tibberApiClient = tibberApiClient;
    }

    public async Task AdjustHeat(decimal accumulatedConsumptionLastHour)
    {
        const decimal HOURLY_POWER_LIMIT = 2m;

        var heater = await _heaterReposiory.GetAsync();

        var currentHeatIsBelowZero = heater.Heat < 0;

        if (accumulatedConsumptionLastHour >= HOURLY_POWER_LIMIT && !currentHeatIsBelowZero)
        {
            await SeatHeatInPump(-1);
        }
        else if (accumulatedConsumptionLastHour < HOURLY_POWER_LIMIT && currentHeatIsBelowZero)
        {
            await SeatHeatInPump(0);
        }
        else
        {
            _logger.LogDebug("No need to adjust the heat. Current heat: {CurrentHeat}. Last update: {HeatLatestUpdate}", heater.Heat, heater.LatestUpdate);
        }
    }

    private async Task<bool> SeatHeatInPump(int value)
    {
        if (value < -10 || value > 10)
        {
            throw new ArgumentException($"Invalid value: {value}", nameof(value));
        }

        var token = await GetAuthToken();

        var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var requestBody = new DevicePoint()
        {
            Value = value
        };

        var deviceId = "emmy-r-208006-20240516-06605519022003-54-10-ec-c4-ca-9a";
        var url = $"v2/devices/{deviceId}/points/47011";

        _logger.LogInformation($"Calling endpoint {url}. Setting heat to {value}");

        var response = await httpClient.PutAsJsonAsync(url, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Sucessfully set heater temprature to {value}. Response: {responseContent}");

        await _heaterReposiory.UpdateAsync(new Heater { Heat = value });

        return true;
    }

    private async Task<Token> GetAuthToken()
    {
        HttpClient httpClient = GetHttpClient();

        var postData = new Dictionary<string, string>
            {
                { "client_id", "My-Uplink-Web" },
                { "username", "Me@mattiasmorell.se" },
                { "password", "" },
                { "grant_type", "password" }
            };

        var content = new FormUrlEncodedContent(postData);

        var authResponse = await httpClient.PostAsync("oauth/token", content);

        var authResponseContentString = await authResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Token>(authResponseContentString)
            ?? throw new HttpRequestException("Deserialize reponse error: " + authResponseContentString);
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://internalapi.myuplink.com/");
        return httpClient;
    }
}
