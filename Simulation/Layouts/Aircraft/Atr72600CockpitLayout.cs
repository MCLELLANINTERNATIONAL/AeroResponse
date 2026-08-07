using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts.Aircraft;

public static class Atr72600CockpitLayout
{
    public static CockpitLayoutDefinition Create()
    {
        return new CockpitLayoutDefinition
        {
            AircraftId = 0,

            Key = "atr-72-600-standard",

            Name = "ATR 72-600",

            Columns = 4,

            Rows = 2,

            Instruments =
            [
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
                            MaximumValue = 350,
                            Unit = "knots"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.ArtificialHorizon,
                    GridRow = 1,
                    GridColumn = 2,
                    RowSpan = 2,

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
                    Type = InstrumentType.HeadingIndicator,
                    GridRow = 1,
                    GridColumn = 3,

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
                    Type = InstrumentType.Altimeter,
                    GridRow = 1,
                    GridColumn = 4,

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
                            MaximumValue = 25_000,
                            Unit = "feet"
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
                            MinimumValue = -4_000,
                            MaximumValue = 4_000,
                            Unit = "feet per minute"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.TurnCoordinator,
                    GridRow = 2,
                    GridColumn = 4,

                    IsVoiceControllable = false
                }
            ],

            Airspeed = new()
            {
                MinimumSpeed = 0,
                MaximumSpeed = 350,

                MinAirspeedAngle = -120,
                MaxAirspeedAngle = 120,

                WhiteArcStart = 85,
                WhiteArcEnd = 180,

                GreenArcStart = 100,
                GreenArcEnd = 250,

                YellowArcStart = 250,
                YellowArcEnd = 320,

                NeverExceedSpeed = 330
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
                MinimumVerticalSpeed = -4000,
                MaximumVerticalSpeed = 4000,

                LagSeconds = 1,

                CalibrationPoints =
                [
                    new(-4000, -235),
                    new(-3000, -200),
                    new(-2000, -160),
                    new(-1000, -125),
                    new(0, -90),
                    new(1000, -55),
                    new(2000, -20),
                    new(3000, 20),
                    new(4000, 55)
                ]
            },

            EngineCount = 2,

            DefaultState = new()
            {
                CruiseAirspeed = 275,
                CruiseAltitude = 17_000,
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