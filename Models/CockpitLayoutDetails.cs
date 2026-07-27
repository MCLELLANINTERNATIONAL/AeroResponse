using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Models;

public class CockpitLayoutDetails
{
    public int AircraftId { get; set; }

    public int Rows { get; set; } = 2;

    public int Columns { get; set; } = 3;

    public List<InstrumentDefinition> Instruments { get; set; } = [];

    public int EngineCount { get; set; }

    public AirspeedIndicatorLayout Airspeed { get; set; } = new();
    public ArtificialHorizonLayout ArtificialHorizon { get; set; } = new();
    public VerticalSpeedIndicatorLayout VSI { get; set; } = new();

    public AircraftStateDefaults DefaultState { get; set; } = new();
}