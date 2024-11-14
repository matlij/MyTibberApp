namespace MyTibber.Common.Models;

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

public enum ComfortMode
{
    Economy = 0,
    Normal = 1,
    Luxury = 2
}