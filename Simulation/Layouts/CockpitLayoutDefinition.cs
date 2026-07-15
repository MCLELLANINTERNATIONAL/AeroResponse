using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts;

public class CockpitLayoutDefinition
{
    public int AircraftId { get; set; }
    
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = "";

    public int Columns { get; set; }

    public int Rows { get; set; }

    public List<InstrumentDefinition> Instruments { get; set; } = [];
    public int EngineCount { get; set; }

    public AirspeedIndicatorLayout Airspeed { get; set; } = new();
    public ArtificialHorizonLayout ArtificialHorizon { get; set; } = new();
    public VerticalSpeedIndicatorLayout VSI { get; set; } = new();

    public AircraftStateDefaults DefaultState { get; set; } = new();
}