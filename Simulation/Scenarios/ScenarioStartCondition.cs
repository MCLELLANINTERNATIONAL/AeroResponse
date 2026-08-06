namespace AeroResponse.Simulation.Scenarios;

public class ScenarioStartCondition
{
    public double? MinimumAltitude { get; set; }
    public double? MaximumAltitude { get; set; }

    public double? MinimumAirspeed { get; set; }
    public double? MaximumAirspeed { get; set; }

    public double? MinimumVerticalSpeed { get; set; }
    public double? MaximumVerticalSpeed { get; set; }

    public double? MinimumAverageEnginePower { get; set; }

    public double? MinimumFuelPercentage { get; set; }

    public double? MinimumHydraulicPressure { get; set; }

    public string? RequiredFlightPhase { get; set; }

    public List<string> AllowedFlightPhases { get; set; } = [];

    public bool? RequiresEnginesRunning { get; set; }

    public bool? RequiresAircraftAirborne { get; set; }

    public bool? RequiresElectricalSystemOnline { get; set; }

    public bool? RequiresRetractableLandingGear { get; set; }
}