using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTibber.Common.Models;
using MyTibber.Common.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyTibber.Common.Repositories;

public class HeatpumpClient(IHttpClientFactory httpClientFactory, IOptions<UpLinkCredentialsOptions> upLinkCredentialsOptions, ILogger<HeatpumpClient> logger)
{
    private const int HeatingOffsetPoint = 47011;
    private const int ComfortModePoint = 47041;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly UpLinkCredentialsOptions _upLinkCredentialsOptions = upLinkCredentialsOptions.Value;
    private readonly ILogger<HeatpumpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DataPointDto> GetCurrentHeat()
    {
        var httpClient = await GetHttpClientWithAuthHeader();

        var uri = $"{GetInternalUplinkAbsolutePath()}?parameters={HeatingOffsetPoint}";

        var response = await httpClient.GetFromJsonAsync<IEnumerable<DataPointDto>>(uri);

        var result = response?.FirstOrDefault() ?? throw new InvalidOperationException($"Failed to get current heat from the my uplink API. URL: {uri}");

        return result ?? throw new InvalidOperationException($"Failed to get current heat.");
    }

    public async Task<bool> UpdateHeat(int value, CancellationToken cancellationToken)
    {
        if (value < -10 || value > 10)
        {
            throw new ArgumentException($"Invalid value: {value}. Must be between -10 and 10", nameof(value));
        }

        var httpClient = await GetHttpClientWithAuthHeader();

        var requestBody = new Dictionary<string, object>
        {
            { HeatingOffsetPoint.ToString(), value }
        };

        var response = await httpClient.PatchAsJsonAsync(GetInternalUplinkAbsolutePath(), requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to update heat. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);

            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Successfully set heater temprature to {value}. Response: {responseContent}");

        return true;
    }

    public async Task<bool> UpdateComfortMode(ComfortMode value, CancellationToken cancellationToken)
    {
        var httpClient = await GetHttpClientWithAuthHeader();

        var requestBody = new Dictionary<string, object>
        {
            { ComfortModePoint.ToString(), value }
        };

        var response = await httpClient.PatchAsJsonAsync(GetInternalUplinkAbsolutePath(), requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to set comfort mode. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);

            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation($"Successfully set comfort mode to {value}. Response: {responseContent}");

        return true;
    }

    private async Task<Token> GetAuthToken()
    {
        var httpClient = GetHttpClient();

        var postData = new Dictionary<string, string>
        {
            { "client_id", _upLinkCredentialsOptions.ClientIdentifier },
            { "client_secret", _upLinkCredentialsOptions.ClientSecret },
            { "grant_type", "client_credentials" }
        };

        var content = new FormUrlEncodedContent(postData);

        var authResponse = await httpClient.PostAsync("oauth/token", content);
        if (!authResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Auth failed with status {authResponse.StatusCode}");
        }

        var authResponseContentString = await authResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Token>(authResponseContentString)
            ?? throw new HttpRequestException("Deserialize reponse error: " + authResponseContentString);
    }

    private HttpClient GetHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.myuplink.com/");

        return httpClient;
    }

    private async Task<HttpClient> GetHttpClientWithAuthHeader()
    {
        var httpClient = GetHttpClient();

        var token = await GetAuthToken();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        return httpClient;
    }

    private static string GetInternalUplinkAbsolutePath()
    {
        const string DEVICE_ID = "emmy-r-208006-20240516-06605519022003-54-10-ec-c4-ca-9a";

        return $"v2/devices/{DEVICE_ID}/points";
    }
}
