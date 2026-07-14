using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class FuelLeakScenario : ISimulationScenario
{
    public int ScenarioId => 6;

    public string ScenarioType => "Fuel Leak";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 260,
            Altitude = 22000,
            Heading = 300,
            Engines =
            [
                new EngineState { Number = 1, Power = 90, Running = true, FuelPercentage = 48 },
                new EngineState { Number = 2, Power = 90, Running = true, FuelPercentage = 48 }
            ],
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