using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts;

public class CockpitLayoutDefinition
{
    public string Name { get; set; } = "";

    public int Columns { get; set; }

    public int Rows { get; set; }

    public List<InstrumentDefinition> Instruments { get; set; } = [];
    public int EngineCount { get; set; }

    public AirspeedIndicatorLayout Airspeed { get; set; } = new();
    public ArtificialHorizonLayout ArtificialHorizon { get; set; } = new();
    public VerticalSpeedIndicatorLayout VSI { get; set; } = new();
}