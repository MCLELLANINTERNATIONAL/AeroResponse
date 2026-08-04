namespace AeroResponse.Models;

public enum LandingGearKind
{
    FixedTricycle,
    Tailwheel,
    RetractableTricycle,
    MultiBogey,
    Tandem,
    Floats,
    Skis
}

public enum LandingGearPosition
{
    Nose,
    LeftMain,
    RightMain,
    Tail,
    Custom
}

public enum LandingGearStatusValue
{
    UpAndLocked,
    Moving,
    DownAndLocked,
    Unsafe,
    Unknown
}

public sealed class LandingGearUnit
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public LandingGearPosition Position { get; set; }
    public LandingGearStatusValue Status { get; set; }
    public int Order { get; set; }
}

public sealed class AircraftLandingGearConfig
{
    public LandingGearKind Kind { get; set; }
    public List<LandingGearUnit> Units { get; set; } = new();
}

public enum FireDetectionStatus
{
    Normal,
    Caution,
    Warning,
    Suppressed,
    Extinguished
}

public enum FireDetectionZone
{
    Cockpit,
    Cabin,
    Engine1,
    Engine2,
    Engine3,
    Engine4,
    Apu
}

public sealed class FireDetectionUnit
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public FireDetectionZone Zone { get; set; }
    public FireDetectionStatus Status { get; set; }
    public int Order { get; set; }
}

public class Aircraft
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxAltitude { get; set; }
    public int CruiseSpeed { get; set; }
    public int EngineCount { get; set; }
    public int FuelTankCount { get; set; }
    public int BrakeCount { get; set; }
    public string CockpitLayoutKey { get; set; } = string.Empty;
    public AircraftLandingGearConfig LandingGearConfig { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}