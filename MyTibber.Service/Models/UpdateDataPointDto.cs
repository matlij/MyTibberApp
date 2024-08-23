namespace MyTibber.Service.Models;

public class UpdateDataPointDto
{
    public object? Value { get; set; }
    public string? Unit { get; set; }
}

public class DataPointDto
{
    public string? ParameterId { get; set; }
    public int RawValue { get; set; }
    public string? Kind { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public bool IsPending { get; set; }
    public object? PendingValue { get; set; }
    public DateTime Timestamp { get; set; }
    public object? MinVal { get; set; }
    public object? MaxVal { get; set; }
}