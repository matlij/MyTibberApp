using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTibber.Service.Models;
using MyTibber.Service.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyTibber.Service.Services;

public class HeaterReposiory
{
    private const int HEAT_POINT = 47011;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly UpLinkCredentialsOptions _upLinkCredentialsOptions;
    private readonly ILogger<HeaterReposiory> _logger;

    public HeaterReposiory(IHttpClientFactory httpClientFactory, IMemoryCache cache, IOptions<UpLinkCredentialsOptions> upLinkCredentialsOptions, ILogger<HeaterReposiory> logger)
    {
        if (upLinkCredentialsOptions is null)
        {
            throw new ArgumentNullException(nameof(upLinkCredentialsOptions));
        }

        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _memoryCache = cache ?? throw new ArgumentNullException(nameof(cache));
        _upLinkCredentialsOptions = upLinkCredentialsOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DataPointDto> GetCurrentHeat()
    {
        var result = await _memoryCache.GetOrCreateAsync(HEAT_POINT, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

            var httpClient = await GetHttpClientWithAuthHeader();

            var uri = $"{GetInternalUplinkAbsolutePath()}?parameters={HEAT_POINT}";

            var response = await httpClient.GetFromJsonAsync<IEnumerable<DataPointDto>>(uri);

            return response?.FirstOrDefault() 
                ?? throw new InvalidOperationException($"Failed to get current heat from the my uplink API. URL: {uri}");
        });

        return result ?? throw new InvalidOperationException($"Failed to get current heat.");
    }

    public async Task<bool> UpdateHeat(int value)
    {
        if (value < -10 || value > 10)
        {
            throw new ArgumentException($"Invalid value: {value}", nameof(value));
        }

        var httpClient = await GetHttpClientWithAuthHeader();

        var requestBody = new UpdateDataPointDto()
        {
            Value = value
        };

        var uri = $"{GetInternalUplinkAbsolutePath()}/{HEAT_POINT}";

        _logger.LogInformation($"Calling endpoint {uri}. Setting heat to {value}");

        var response = await httpClient.PutAsJsonAsync(uri, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Successfully set heater temprature to {value}. Response: {responseContent}");

        _memoryCache.Remove(HEAT_POINT);

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

    private string GetInternalUplinkAbsolutePath()
    {
        const string DEVICE_ID = "emmy-r-208006-20240516-06605519022003-54-10-ec-c4-ca-9a";
        
        return $"v2/devices/{DEVICE_ID}/points";
    }
}
