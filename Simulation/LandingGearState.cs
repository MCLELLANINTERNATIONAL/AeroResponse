using AeroResponse.Models;

namespace AeroResponse.Simulation;

public class LandingGearState
{
    public int Number { get; set; }

    public string Label { get; set; } = string.Empty;

    public LandingGearPosition Position { get; set; }

    public LandingGearStatusValue Status { get; set; }
}