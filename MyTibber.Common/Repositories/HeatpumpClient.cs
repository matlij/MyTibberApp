using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTibber.Common.Models;
using MyTibber.Common.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyTibber.Common.Repositories;

public class HeatpumpClient(IHttpClientFactory httpClientFactory, IOptions<UpLinkCredentialsOptions> upLinkCredentialsOptions, ILogger<HeatpumpClient> logger)
{
    private const int HEAT_POINT = 47011;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly UpLinkCredentialsOptions _upLinkCredentialsOptions = upLinkCredentialsOptions.Value;
    private readonly ILogger<HeatpumpClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DataPointDto> GetCurrentHeat()
    {
        var httpClient = await GetHttpClientWithAuthHeader();

        var uri = $"{GetInternalUplinkAbsolutePath()}?parameters={HEAT_POINT}";

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

        var requestBody = new UpdateDataPointDto()
        {
            Value = value
        };

        var uri = $"{GetInternalUplinkAbsolutePath()}/{HEAT_POINT}";

        _logger.LogInformation($"Calling endpoint {uri}. Setting heat to {value}");

        var response = await httpClient.PutAsJsonAsync(uri, requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Successfully set heater temprature to {value}. Response: {responseContent}");

        return true;
    }

    private async Task<Token> GetAuthToken()
    {
        HttpClient httpClient = GetHttpClient();

        var postData = new Dictionary<string, string>
        {
            { "client_id", "My-Uplink-Web" },
            { "username", _upLinkCredentialsOptions.Username },
            { "password", _upLinkCredentialsOptions.Password },
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
