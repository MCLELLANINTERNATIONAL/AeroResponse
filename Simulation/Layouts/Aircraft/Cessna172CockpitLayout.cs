using System.Diagnostics.Metrics;
using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts.Aircraft;

public static class Cessna172CockpitLayout
{
    public static CockpitLayoutDefinition Create()
    {
        return new CockpitLayoutDefinition
        {
            AircraftId = 1,
            
            Key = "cessna-172-standard",

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
                },

                new()
                {
                    Type = InstrumentType.ArtificialHorizon,
                    GridRow = 1,
                    GridColumn = 2
                },

                new()
                {
                    Type = InstrumentType.TurnCoordinator,
                    GridRow = 2,
                    GridColumn = 1
                },

                new()
                {
                    Type = InstrumentType.HeadingIndicator,
                    GridRow = 2,
                    GridColumn = 2
                },

                new()
                {
                    Type = InstrumentType.VerticalSpeedIndicator,
                    GridRow = 2,
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

            ArtificialHorizon = new()
            {
                MinimumPitch = -30, 
                MaximumPitch = 30,

                MinimumBank = -100,
                MaximumBank = 100
            },

            VSI = new()
            {
                MinimumVerticalSpeed = -2000, // Feet Per Minute
                MaximumVerticalSpeed = 2000,

                LagSeconds = 6,

                CalibrationPoints =
                [
                    new(-2000, -235), // Where the Numbers sit on the VSI Instrument
                    new(-1500, -200),
                    new(-1000, -160),
                    new(-500, -125),
                    new(0, -90),
                    new(500, -55),
                    new(1000, -20),
                    new(1500, 20),
                    new(2000, 55)
                ]
            },

            EngineCount = 1,

            DefaultState = new()
            {
                CruiseAirspeed = 115,
                CruiseAltitude = 3000,
                DefaultHeading = 240,
                DefaultVerticalSpeed = 0,
                DefaultPitch = 2,
                DefaultBank = 0,
                NormalEnginePower = 75,
                FuelPercentage = 75
            }
        };
    }
}