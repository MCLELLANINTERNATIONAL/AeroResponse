namespace AeroResponse.Simulation;

public class CockpitState
{
    public int Airspeed { get; set; } = 250;

    public int Altitude { get; set; } = 12000;

    public int Heading { get; set; } = 270;

    public int VerticalSpeed { get; set; } = 0;

    public int EngineOnePower { get; set; } = 90;

    public int EngineTwoPower { get; set; } = 90;

    public int FuelPercentage { get; set; } = 75;

    public bool EngineFire { get; set; }

    public bool FuelCutoff { get; set; }

    public bool FireSuppressionActivated { get; set; }

    public string AlertMessage { get; set; } = "Systems Normal";
}