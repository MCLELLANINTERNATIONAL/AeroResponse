using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts.Aircraft;

public static class Cessna172CockpitLayout
{
    public static CockpitLayoutDefinition Create()
    {
        return new CockpitLayoutDefinition
        {
            Name = "Cessna 172",

            Columns = 3,

            Rows = 2,

            Instruments = [
                new()
                {
                    Type = InstrumentType.AirspeedIndicator,
                    GridRow = 1,
                    GridColumn = 1
                },

                new()
                {
                    Type = InstrumentType.Altimeter,
                    GridRow = 1,
                    GridColumn = 3
                }
            ],

            Airspeed = new()
            {
                MinimumSpeed = 0,
                MaximumSpeed = 200,

                MinAirspeedAngle = -120,
                MaxAirspeedAngle = 120,

                WhiteArcStart = 40,
                WhiteArcEnd = 85,

                GreenArcStart = 48,
                GreenArcEnd = 129,

                YellowArcStart = 129,
                YellowArcEnd = 163,

                NeverExceedSpeed = 163
            },

            EngineCount = 1
        };
    }
}