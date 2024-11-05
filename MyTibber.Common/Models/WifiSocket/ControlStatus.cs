using System.Text.Json.Serialization;

namespace MyTibber.Common.Models.WifiSocket;

public class SetTempratureResponse
{
    public string Status { get; set; } = string.Empty;
}

public class SetTemprature
{
    public string Type { get; set; } = "Normal";
    public int Value { get; set; }
}

public class ControllStatus
{
    [JsonPropertyName("ambient_temperature")]
    public double AmbientTemperature { get; set; }

    [JsonPropertyName("humidity")]
    public double Humidity { get; set; }

    [JsonPropertyName("current_power")]
    public int CurrentPower { get; set; }

    [JsonPropertyName("control_signal")]
    public int ControlSignal { get; set; }

    [JsonPropertyName("lock_active")]
    public string? LockActive { get; set; }

    [JsonPropertyName("open_window_active_now")]
    public string? OpenWindowActiveNow { get; set; }

    [JsonPropertyName("raw_ambient_temperature")]
    public double RawAmbientTemperature { get; set; }

    [JsonPropertyName("set_temperature")]
    public int SetTemperature { get; set; }

    [JsonPropertyName("switched_on")]
    public bool SwitchedOn { get; set; }

    [JsonPropertyName("connected_to_cloud")]
    public bool ConnectedToCloud { get; set; }

    [JsonPropertyName("operation_mode")]
    public string? OperationMode { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}