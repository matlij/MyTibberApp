using Microsoft.Extensions.Logging;
using MyTibber.Service.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyTibber.Service.Services;

public class HeaterService
{
    private readonly ILogger<HeaterService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HeaterReposiory _heaterReposiory;

    public HeaterService(ILogger<HeaterService> logger, IHttpClientFactory httpClientFactory, HeaterReposiory heaterReposiory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _heaterReposiory = heaterReposiory;
    }

    public async Task<bool> AdjustHeat(decimal accumulatedConsumptionLastHour)
    {
        var heater = await _heaterReposiory.GetAsync();

        var heatIsBelowNormal = heater.Heat < 0;

        if (accumulatedConsumptionLastHour >= 2 && !heatIsBelowNormal)
        {
            await SeatHeatInPump(-3);
        }
        else if (accumulatedConsumptionLastHour < 2 && heatIsBelowNormal)
        {
            await SeatHeatInPump(0);
        }

        return true;
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
        var response = await httpClient.PutAsJsonAsync($"v2/devices/{deviceId}/points/47011", requestBody);

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
