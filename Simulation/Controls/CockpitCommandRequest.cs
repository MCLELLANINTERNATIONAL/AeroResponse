namespace AeroResponse.Simulation.Controls;

public sealed class CockpitCommandRequest
{
    public string RawText { get; init; } = string.Empty;
    public string ControlId { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public double? NumericValue { get; init; }
    public string? Unit { get; init; }
    public double Confidence { get; init; }
}