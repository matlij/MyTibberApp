using Microsoft.Extensions.Logging;

namespace MyTibber.Common.Repositories;

public class WifiSocketsService
{
    private readonly IEnumerable<WifiSocketClient> _wifiSocketClients;
    private readonly ILogger<WifiSocketsService> _logger;

    public WifiSocketsService(
        IEnumerable<WifiSocketClient> radiators,
        ILogger<WifiSocketsService> logger)
    {
        _wifiSocketClients = radiators ?? throw new ArgumentNullException(nameof(radiators));
        _logger = logger;
    }

    public async Task UpdateAllClients(int temperature, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var client in _wifiSocketClients)
        {
            tasks.Add(UpdateWifiSocketSafely(client, temperature, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task UpdateWifiSocketSafely(WifiSocketClient client, int temperature, CancellationToken cancellationToken)
    {
        try
        {
            var success = await client.UpdateHeat(temperature, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Failed to update temperature for radiator {RadiatorName}", client.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating radiator {RadiatorName}", client.Name);
        }
    }
}
