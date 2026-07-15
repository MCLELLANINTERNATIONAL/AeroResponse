namespace AeroResponse.Models;

public class SimulationSelection
{
    public string AircraftKey { get; set; } = string.Empty;

    public string ScenarioType { get; set; } = string.Empty;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(AircraftKey) &&
        !string.IsNullOrWhiteSpace(ScenarioType);
}