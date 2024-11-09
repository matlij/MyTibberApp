using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTibber.Common.Models.WifiSocket;
using MyTibber.Common.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyTibber.Common.Repositories;

public class WifiSocketClient(IHttpClientFactory httpClientFactory, IOptions<WifiSocketOptions> wifiSocketOptions, ILogger<WifiSocketClient> logger)
{
    private const string STATUS_OK = "ok";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly WifiSocketOptions _wifiSocketOptions = wifiSocketOptions?.Value ?? throw new ArgumentNullException(nameof(wifiSocketOptions));
    private readonly ILogger<WifiSocketClient> _logger = logger;
    
    public string Name => _wifiSocketOptions.Name;

    public async Task<bool> UpdateHeat(int value, CancellationToken cancellationToken)
    {
        if (value < 5 || value > 20)
        {
            throw new ArgumentException($"Invalid value: {value}. Must be between 5 and 20", nameof(value));
        }

        var status = await GetStatus();
        if (status.Status != STATUS_OK)
        {
            _logger.LogWarning($"Radiator is not in a valid state for temperature update. Current status: {status.Status}. Expected status: {STATUS_OK}");
            return false;
        }

        return await SetTemprature(value, cancellationToken);
    }

    public async Task<ControllStatus> GetStatus()
    {
        var httpClient = GetHttpClient();

        var response = await httpClient.GetFromJsonAsync<ControllStatus>("control-status");

        return response
            ?? throw new HttpRequestException("Failed to retrieve control status from radiator endpoint.");
    }

    private async Task<bool> SetTemprature(int value, CancellationToken cancellationToken)
    {
        var httpClient = GetHttpClient();

        var requestBody = new SetTemprature()
        {
            Value = value
        };
        var requestContent = new StringContent(JsonSerializer.Serialize(requestBody));

        var response = await httpClient.PostAsync("set-temperature", requestContent, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
            _logger.LogWarning("Set Wifi-socket temparature failed. Response code: {statuscode}. Respone message: {message}", response.StatusCode, content);
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var setTempratureResponse = JsonSerializer.Deserialize<SetTempratureResponse>(responseContent);
        if (setTempratureResponse != null && setTempratureResponse.Status != "ok")
        {
            _logger.LogWarning("Set temprature failed. Expected response 'ok' but got '{setTempratureResponse}'", setTempratureResponse);
            return false;
        }

        return true;
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_wifiSocketOptions.BaseAddress);
        return httpClient;
    }
}
