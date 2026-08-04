using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Controls;

public sealed class CockpitControlCatalog
{
    public IReadOnlyList<CockpitControlDefinition> Build(CockpitLayoutDefinition layout)
    {
        var controls = layout.Instruments
            .Select(instrument => BuildInstrumentControl(instrument, layout))
            .ToList();

        for (var engineNumber = 1;
             engineNumber <= Math.Max(1, layout.EngineCount);
             engineNumber++)
        {
            controls.AddRange(BuildEngineControls(engineNumber));
        }

        controls.Add(new CockpitControlDefinition
        {
            ControlId = "communication.emergency",
            DisplayName = "Emergency Declaration",
            ControlType = "Communication",
            VoiceAliases = ["emergency declaration", "air traffic control"],
            Commands =
            [
                new()
                {
                    Command = "declare",
                    VoiceAliases = ["declare emergency", "mayday", "send mayday"]
                }
            ]
        });

        return controls
            .GroupBy(control => control.ControlId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static CockpitControlDefinition BuildInstrumentControl(
        InstrumentDefinition instrument,
        CockpitLayoutDefinition layout)
    {
        // Explicit metadata always wins. This is how future instruments register.
        if (!string.IsNullOrWhiteSpace(instrument.ControlId))
        {
            return new CockpitControlDefinition
            {
                ControlId = instrument.ControlId,
                DisplayName = string.IsNullOrWhiteSpace(instrument.DisplayName)
                    ? instrument.Type.ToString()
                    : instrument.DisplayName,
                ControlType = instrument.Type.ToString(),
                IsVoiceControllable = instrument.IsVoiceControllable,
                VoiceAliases = instrument.VoiceAliases,
                Commands = instrument.VoiceCommands
            };
        }

        return instrument.Type switch
        {
            InstrumentType.AirspeedIndicator => Numeric(
                "flight.airspeed", "Airspeed", ["airspeed", "speed"],
                0, layout.Airspeed.MaximumSpeed, "knots"),

            InstrumentType.Altimeter => Numeric(
                "flight.altitude", "Altitude", ["altitude", "height"],
                0, 60_000, "feet"),

            InstrumentType.HeadingIndicator => Numeric(
                "flight.heading", "Heading", ["heading", "course"],
                0, 359, "degrees"),

            InstrumentType.VerticalSpeedIndicator => Numeric(
                "flight.vertical-speed", "Vertical Speed",
                ["vertical speed", "rate of climb", "rate of descent"],
                layout.VSI.MinimumVerticalSpeed,
                layout.VSI.MaximumVerticalSpeed,
                "feet per minute"),

            InstrumentType.ArtificialHorizon => new CockpitControlDefinition
            {
                ControlId = "flight.attitude",
                DisplayName = "Aircraft Attitude",
                ControlType = "Attitude",
                VoiceAliases = ["attitude", "pitch", "bank"],
                Commands =
                [
                    new()
                    {
                        Command = "set-pitch",
                        VoiceAliases = ["set pitch", "pitch"],
                        RequiresNumericValue = true,
                        MinimumValue = -30,
                        MaximumValue = 30,
                        Unit = "degrees"
                    },
                    new()
                    {
                        Command = "set-bank",
                        VoiceAliases = ["set bank", "bank"],
                        RequiresNumericValue = true,
                        MinimumValue = -90,
                        MaximumValue = 90,
                        Unit = "degrees"
                    }
                ]
            },

            _ => new CockpitControlDefinition
            {
                ControlId = $"instrument.{instrument.Type.ToString().ToLowerInvariant()}",
                DisplayName = instrument.Type.ToString(),
                ControlType = instrument.Type.ToString(),
                IsVoiceControllable = false
            }
        };
    }

    private static CockpitControlDefinition Numeric(
        string id, string name, List<string> aliases,
        double minimum, double maximum, string unit) => new()
    {
        ControlId = id,
        DisplayName = name,
        ControlType = "Numeric",
        VoiceAliases = aliases,
        Commands =
        [
            new()
            {
                Command = "set",
                VoiceAliases = [$"set {name.ToLowerInvariant()}", name.ToLowerInvariant()],
                RequiresNumericValue = true,
                MinimumValue = minimum,
                MaximumValue = maximum,
                Unit = unit
            }
        ]
    };

    private static IEnumerable<CockpitControlDefinition> BuildEngineControls(int number)
    {
        yield return new CockpitControlDefinition
        {
            ControlId = $"engine.{number}.thrust",
            DisplayName = $"Engine {number} Thrust",
            ControlType = "EngineThrust",
            EngineNumber = number,
            VoiceAliases = [$"engine {number} thrust", $"engine {number} power"],
            Commands =
            [
                new()
                {
                    Command = "idle",
                    VoiceAliases = [$"set engine {number} thrust idle", $"engine {number} idle"]
                },
                new()
                {
                    Command = "set",
                    VoiceAliases = [$"set engine {number} thrust", $"engine {number} power"],
                    RequiresNumericValue = true,
                    MinimumValue = 0,
                    MaximumValue = 100,
                    Unit = "percent"
                }
            ]
        };

        yield return new CockpitControlDefinition
        {
            ControlId = $"engine.{number}.fuel",
            DisplayName = $"Engine {number} Fuel",
            ControlType = "EngineFuel",
            EngineNumber = number,
            IsSafetyCritical = true,
            VoiceAliases = [$"engine {number} fuel"],
            Commands =
            [
                new()
                {
                    Command = "cutoff",
                    VoiceAliases = [$"cut off engine {number} fuel", $"engine {number} fuel cutoff"]
                },
                new()
                {
                    Command = "on",
                    VoiceAliases = [$"engine {number} fuel on", $"restore engine {number} fuel"]
                }
            ]
        };

        yield return new CockpitControlDefinition
        {
            ControlId = $"engine.{number}.fire-handle",
            DisplayName = $"Engine {number} Fire Handle",
            ControlType = "FireHandle",
            EngineNumber = number,
            IsSafetyCritical = true,
            VoiceAliases = [$"engine {number} fire handle", $"fire handle {number}"],
            Commands =
            [
                new()
                {
                    Command = "pull",
                    VoiceAliases = [$"pull engine {number} fire handle", $"pull fire handle {number}"]
                }
            ]
        };

        yield return new CockpitControlDefinition
        {
            ControlId = $"engine.{number}.fire-bottle",
            DisplayName = $"Engine {number} Fire Bottle",
            ControlType = "FireBottle",
            EngineNumber = number,
            IsSafetyCritical = true,
            VoiceAliases = [$"engine {number} fire bottle", $"fire bottle {number}"],
            Commands =
            [
                new()
                {
                    Command = "discharge",
                    VoiceAliases = [$"discharge engine {number} fire bottle", $"discharge fire bottle {number}"]
                }
            ]
        };
    }
}