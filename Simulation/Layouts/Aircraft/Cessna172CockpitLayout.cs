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
                    GridColumn = 1,

                    ControlId = "flight.airspeed",
                    DisplayName = "Airspeed",
                    IsVoiceControllable = true,

                    VoiceAliases =
                    [
                        "airspeed",
                        "speed",
                        "indicated airspeed"
                    ],

                    VoiceCommands =
                    [
                        new CockpitControlCommandDefinition
                        {
                            Command = "set",

                            VoiceAliases =
                            [
                                "set airspeed",
                                "set speed",
                                "set indicated airspeed"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = 0,
                            MaximumValue = 200,
                            Unit = "knots"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.Altimeter,
                    GridRow = 1,
                    GridColumn = 3,

                    ControlId = "flight.altitude",
                    DisplayName = "Altitude",
                    IsVoiceControllable = true,

                    VoiceAliases =
                    [
                        "altitude",
                        "altimeter"
                    ],

                    VoiceCommands =
                    [
                        new CockpitControlCommandDefinition
                        {
                            Command = "set",

                            VoiceAliases =
                            [
                                "set altitude",
                                "set altimeter",
                                "select altitude"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = 0,
                            MaximumValue = 14_000,
                            Unit = "feet"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.ArtificialHorizon,
                    GridRow = 1,
                    GridColumn = 2,

                    ControlId = "flight.attitude",
                    DisplayName = "Aircraft Attitude",
                    IsVoiceControllable = true,

                    VoiceAliases =
                    [
                        "attitude",
                        "artificial horizon",
                        "pitch and bank"
                    ],

                    VoiceCommands =
                    [
                        new CockpitControlCommandDefinition
                        {
                            Command = "set-pitch",

                            VoiceAliases =
                            [
                                "set pitch",
                                "pitch up",
                                "pitch down"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = -30,
                            MaximumValue = 30,
                            Unit = "degrees"
                        },

                        new CockpitControlCommandDefinition
                        {
                            Command = "set-bank",

                            VoiceAliases =
                            [
                                "set bank",
                                "bank left",
                                "bank right"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = -100,
                            MaximumValue = 100,
                            Unit = "degrees"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.TurnCoordinator,
                    GridRow = 2,
                    GridColumn = 1,

                    IsVoiceControllable = false
                },

                new()
                {
                    Type = InstrumentType.HeadingIndicator,
                    GridRow = 2,
                    GridColumn = 2,

                    ControlId = "flight.heading",
                    DisplayName = "Heading",
                    IsVoiceControllable = true,

                    VoiceAliases =
                    [
                        "heading",
                        "heading indicator",
                        "direction"
                    ],

                    VoiceCommands =
                    [
                        new CockpitControlCommandDefinition
                        {
                            Command = "set",

                            VoiceAliases =
                            [
                                "set heading",
                                "select heading",
                                "turn to heading"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = 0,
                            MaximumValue = 359,
                            Unit = "degrees"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.VerticalSpeedIndicator,
                    GridRow = 2,
                    GridColumn = 3,

                    ControlId = "flight.vertical-speed",
                    DisplayName = "Vertical Speed",
                    IsVoiceControllable = true,

                    VoiceAliases =
                    [
                        "vertical speed",
                        "v s i",
                        "rate of climb",
                        "rate of descent"
                    ],

                    VoiceCommands =
                    [
                        new CockpitControlCommandDefinition
                        {
                            Command = "set",

                            VoiceAliases =
                            [
                                "set vertical speed",
                                "set rate of climb",
                                "set rate of descent"
                            ],

                            RequiresNumericValue = true,
                            MinimumValue = -2_000,
                            MaximumValue = 2_000,
                            Unit = "feet per minute"
                        }
                    ]
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