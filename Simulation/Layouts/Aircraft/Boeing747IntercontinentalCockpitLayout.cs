using AeroResponse.Models;

namespace AeroResponse.Simulation.Layouts.Aircraft;

public static class Boeing747IntercontinentalCockpitLayout
{
    public static CockpitLayoutDefinition Create()
    {
        return new CockpitLayoutDefinition
        {
            AircraftId = 0,

            Key = "boeing-747-standard",

            Name = "Boeing 747-8 Intercontinental",

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
                            MaximumValue = 600,
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
                    RowSpan = 2,

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
                            MaximumValue = 43_100,
                            Unit = "feet"
                        }
                    ]
                },

                new()
                {
                    Type = InstrumentType.VerticalSpeedIndicator,
                    GridRow = 2,
                    GridColumn = 1,

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
                            MinimumValue = -6_000,
                            MaximumValue = 6_000,
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
                MaximumSpeed = 600,

                MinAirspeedAngle = -120,
                MaxAirspeedAngle = 120,

                WhiteArcStart = 120,
                WhiteArcEnd = 220,

                GreenArcStart = 140,
                GreenArcEnd = 420,

                YellowArcStart = 420,
                YellowArcEnd = 540,

                NeverExceedSpeed = 570
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
                MinimumVerticalSpeed = -6000,
                MaximumVerticalSpeed = 6000,

                LagSeconds = 1,

                CalibrationPoints =
                [
                    new(-6000, -235),
                    new(-4500, -200),
                    new(-3000, -160),
                    new(-1500, -125),
                    new(0, -90),
                    new(1500, -55),
                    new(3000, -20),
                    new(4500, 20),
                    new(6000, 55)
                ]
            },

            EngineCount = 4,

            DefaultState = new()
            {
                CruiseAirspeed = 493,
                CruiseAltitude = 35_000,
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