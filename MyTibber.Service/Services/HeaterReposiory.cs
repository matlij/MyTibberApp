using System.Text.Json;
using MyTibber.Service.Models;

namespace MyTibber.Service.Services;

public class HeaterReposiory
{
    private const string FILE_PATH = "heatinglog.txt";

    public async Task<bool> UpdateAsync(Heater heater)
    {
        heater.LatestUpdate = DateTime.Now;
        
        var data = JsonSerializer.Serialize(heater);
        await File.WriteAllTextAsync(FILE_PATH, data);

        return true;
    }

    public async Task<Heater> GetAsync()
    {
        var data = await File.ReadAllTextAsync(FILE_PATH);

        var result = JsonSerializer.Deserialize<Heater>(data) 
            ?? throw new InvalidOperationException($"Failed to Deserialize '{data}' to {nameof(Heater)}");

        return result;
    }
}
