namespace AeroResponse.Simulation;

public class EngineState
{
    public int Number { get; init; }

    public int Power { get; set; } = 90;

    public bool Running { get; set; } = true;

    public bool OnFire { get; set; }

    public int FuelPercentage { get; set; } = 75;

    public bool EngineFire { get; set; }

    public bool FuelCutoff { get; set; }

    public bool FireSuppressionActivated { get; set; }
}