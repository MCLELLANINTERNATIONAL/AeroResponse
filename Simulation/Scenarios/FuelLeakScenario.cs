using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class FuelLeakScenario : ISimulationScenario
{
    public int ScenarioId => 6;

    public string ScenarioType => "Fuel Leak";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        var defaults = aircraft.DefaultState;
        var engines = Enumerable.Range(1, aircraft.EngineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = defaults.NormalEnginePower,
                Running = true,
                FuelPercentage = Math.Max(0, defaults.FuelPercentage - 25)
            })
            .ToList();

        return new CockpitState
        {
            Airspeed = defaults.CruiseAirspeed,
            Altitude = defaults.CruiseAltitude,
            Heading = defaults.DefaultHeading,
            VerticalSpeed = defaults.DefaultVerticalSpeed,
            DisplayedVerticalSpeed = defaults.DefaultVerticalSpeed,
            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,
            FuelPercentage = defaults.FuelPercentage,
            Engines = engines,
            AlertMessage = $"{aircraft.Name}: FUEL LEAK DETECTED - FUEL QUANTITY DECREASING"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Maintain aircraft control and monitor fuel", CorrectAction = "Monitor Fuel", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Identify affected fuel system", CorrectAction = "Identify Fuel Leak", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Isolate affected fuel source if required", CorrectAction = "Fuel Cutoff", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Divert to nearest suitable airport", CorrectAction = "Prepare Diversion", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Monitor Fuel")
        {
            foreach (var engine in state.Engines)
            {
                engine.FuelPercentage = Math.Max(0, engine.FuelPercentage - 5);
            }
        }

        if (actionName == "Identify Fuel Leak")
        {
            state.AlertMessage = "LEFT FUEL SYSTEM LEAK SUSPECTED";
        }

        if (actionName == "Fuel Cutoff")
        {
            foreach (var engine in state.Engines)
            {
                engine.FuelCutoff = true;
            }
            state.AlertMessage = "AFFECTED FUEL SYSTEM ISOLATED";
        }

        if (actionName == "Prepare Diversion")
        {
            state.AlertMessage = "DIVERSION PLANNED - LAND AS SOON AS PRACTICAL";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}