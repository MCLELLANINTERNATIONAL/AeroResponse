using Microsoft.EntityFrameworkCore.Storage;

namespace AeroResponse.Simulation;

public class CockpitState
{
    public double Airspeed { get; set; } = 200;

    public double Altitude { get; set; } = 12000;

    public double Heading { get; set; } = 270;

    public double VerticalSpeed { get; set; } = 0;
    
    public double DisplayedVerticalSpeed { get; set; } = 0;

    public double Pitch { get; set; } = 0;

    public double Bank { get; set; } = 0;

    public double Slip { get; set; } = 0;
    
    public double TurnRate { get; set; } = 0;

    public List<EngineState> Engines { get; set; } = [];

    public string AlertMessage { get; set; } = "Systems Normal";

    public double FuelPercentage { get; set; } = 100;
}