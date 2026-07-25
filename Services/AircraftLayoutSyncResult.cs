namespace AeroResponse.Services;

// To Showcase what is different with local app aircraft and database

public class AircraftLayoutSyncResult
{
    public List<string> AddedAircraft { get; } = [];

    public List<AircraftLayoutDifference> Differences { get; } = [];

    public int AddedCount => AddedAircraft.Count;

    public int DifferenceCount => Differences.Count;
}

public class AircraftLayoutDifference
{
    public int AircraftId { get; init; }

    public string CockpitLayoutKey { get; init; } =
        string.Empty;

    public string DatabaseName { get; init; } =
        string.Empty;

    public string LocalLayoutName { get; init; } =
        string.Empty;

    public List<string> DifferentFields { get; init; } = [];
}