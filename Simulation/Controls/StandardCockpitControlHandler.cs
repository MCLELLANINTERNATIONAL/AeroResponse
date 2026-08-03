using AeroResponse.Models;

namespace AeroResponse.Simulation.Controls;

public sealed class StandardCockpitControlHandler : ICockpitControlHandler
{
    public bool CanHandle(CockpitControlDefinition definition) =>
        definition.ControlType is "Numeric" or "Attitude" or "EngineThrust" or
        "EngineFuel" or "FireHandle" or "FireBottle" or "Communication";

    public CockpitCommandResult Execute(CockpitControlDefinition definition,
        CockpitCommandRequest request, CockpitState state) =>
        definition.ControlType switch
        {
            "Numeric" => ExecuteNumeric(definition, request, state),
            "Attitude" => ExecuteAttitude(definition, request, state),
            "EngineThrust" => ExecuteThrust(definition, request, state),
            "EngineFuel" => ExecuteFuel(definition, request, state),
            "FireHandle" => ExecuteFireHandle(definition, state),
            "FireBottle" => ExecuteFireBottle(definition, state),
            "Communication" => CockpitCommandResult.Success(
                "Declare Emergency", "Emergency declaration recorded.", definition.ControlId),
            _ => CockpitCommandResult.Failure("No control handler is registered.")
        };

    private static CockpitCommandResult ExecuteNumeric(CockpitControlDefinition d,
        CockpitCommandRequest r, CockpitState s)
    {
        if (!r.NumericValue.HasValue)
            return CockpitCommandResult.Failure($"{d.DisplayName} requires a value.");

        var command = d.Commands.First(x => x.Command.Equals(r.Command,
            StringComparison.OrdinalIgnoreCase));
        var value = Math.Clamp(r.NumericValue.Value,
            command.MinimumValue ?? double.MinValue,
            command.MaximumValue ?? double.MaxValue);

        switch (d.ControlId)
        {
            case "flight.airspeed": s.Airspeed = value; break;
            case "flight.altitude": s.Altitude = value; break;
            case "flight.heading": s.Heading = ((value % 360) + 360) % 360; value = s.Heading; break;
            case "flight.vertical-speed": s.VerticalSpeed = value; break;
            default: s.DynamicValues[d.ControlId] = value; break;
        }

        return CockpitCommandResult.Success($"{d.DisplayName} Set",
            $"{d.DisplayName} set to {value:0.#} {command.Unit}.", d.ControlId);
    }

    private static CockpitCommandResult ExecuteAttitude(CockpitControlDefinition d,
        CockpitCommandRequest r, CockpitState s)
    {
        if (!r.NumericValue.HasValue)
            return CockpitCommandResult.Failure("Attitude command requires a value.");

        if (r.Command == "set-pitch")
        {
            s.Pitch = Math.Clamp(r.NumericValue.Value, -30, 30);
            return CockpitCommandResult.Success("Set Pitch",
                $"Pitch set to {s.Pitch:0.#} degrees.", d.ControlId);
        }

        s.Bank = Math.Clamp(r.NumericValue.Value, -90, 90);
        return CockpitCommandResult.Success("Set Bank",
            $"Bank set to {s.Bank:0.#} degrees.", d.ControlId);
    }

    private static CockpitCommandResult ExecuteThrust(CockpitControlDefinition d,
        CockpitCommandRequest r, CockpitState s)
    {
        var engine = FindEngine(d, s);
        if (engine is null) return CockpitCommandResult.Failure("Engine not found.");
        engine.Power = r.Command == "idle" ? 0 : Math.Clamp(r.NumericValue ?? engine.Power, 0, 100);
        return CockpitCommandResult.Success($"Set Engine {engine.Number} Thrust",
            $"Engine {engine.Number} thrust set to {engine.Power:0} percent.", d.ControlId);
    }

    private static CockpitCommandResult ExecuteFuel(CockpitControlDefinition d,
        CockpitCommandRequest r, CockpitState s)
    {
        var engine = FindEngine(d, s);
        if (engine is null) return CockpitCommandResult.Failure("Engine not found.");
        engine.FuelCutoff = r.Command == "cutoff";
        var action = engine.FuelCutoff ? $"Engine {engine.Number} Fuel Cutoff" : $"Engine {engine.Number} Fuel On";
        return CockpitCommandResult.Success(action, action + ".", d.ControlId);
    }

    private static CockpitCommandResult ExecuteFireHandle(CockpitControlDefinition d,
        CockpitState s)
    {
        var engine = FindEngine(d, s);
        if (engine is null) return CockpitCommandResult.Failure("Engine not found.");
        engine.Running = false;
        engine.FuelCutoff = true;
        return CockpitCommandResult.Success($"Pull Engine {engine.Number} Fire Handle",
            $"Engine {engine.Number} fire handle pulled.", d.ControlId);
    }

    private static CockpitCommandResult ExecuteFireBottle(CockpitControlDefinition d,
        CockpitState s)
    {
        var engine = FindEngine(d, s);
        if (engine is null) return CockpitCommandResult.Failure("Engine not found.");
        engine.FireSuppressionActivated = true;
        engine.EngineFire = false;
        engine.OnFire = false;
        return CockpitCommandResult.Success($"Discharge Engine {engine.Number} Fire Bottle",
            $"Engine {engine.Number} fire bottle discharged.", d.ControlId);
    }

    private static EngineState? FindEngine(CockpitControlDefinition d, CockpitState s) =>
        s.Engines.FirstOrDefault(engine => engine.Number == d.EngineNumber);
}