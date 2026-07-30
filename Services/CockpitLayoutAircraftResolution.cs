namespace AeroResponse.Services;

public enum AircraftResolutionAction
{
    Modify,
    Delete
}

public sealed class CockpitLayoutAircraftResolution
{
    public int AircraftId { get; init; }

    public AircraftResolutionAction Action { get; init; }

    public string? ReplacementLayoutKey { get; init; }
}