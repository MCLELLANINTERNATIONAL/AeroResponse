using Microsoft.EntityFrameworkCore.Storage;

namespace AeroResponse.Simulation;

public class CockpitState
{
    public int Airspeed { get; set; } = 200;

    public int Altitude { get; set; } = 12000;

    public int Heading { get; set; } = 270;

    public int VerticalSpeed { get; set; } = 0;
    
    public double DisplayedVerticalSpeed { get; set; } = 0;

    public double Pitch { get; set; } = 0;

    public double Bank { get; set; } = 0;

    public double Slip { get; set; } = 0;
    
    public double TurnRate { get; set; } = 0;

    public List<EngineState> Engines { get; set; } = [];

    public string AlertMessage { get; set; } = "Systems Normal";
}