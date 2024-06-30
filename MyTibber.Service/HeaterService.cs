using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyTibber.Service;

internal class Token
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
}

public class DevicePoint
{
    public object? Value { get; set; }
    public string? Unit { get; set; }
}

public class HeaterService
{
    private readonly ILogger<HeaterService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HeaterService(ILogger<HeaterService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SetHeat(int heatValue)
    {
        if (heatValue < -10 || heatValue > 10)
        {
            throw new ArgumentException($"Invalid value: {heatValue}", nameof(heatValue));
        }

        var token = await GetAuthToken();

        var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        var requestBody = new DevicePoint()
        {
            Value = heatValue
        };

        var deviceId = "emmy-r-208006-20240516-06605519022003-54-10-ec-c4-ca-9a";
        var response = await httpClient.PutAsJsonAsync($"v2/devices/{deviceId}/points/47011", requestBody);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Sucessfully set heater temprature to {heatValue}. Response: {responseContent}");

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
