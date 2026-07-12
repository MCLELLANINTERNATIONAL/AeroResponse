using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class FuelLeakScenario : ISimulationScenario
{
    public string ScenarioType => "Fuel Leak";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 260,
            Altitude = 22000,
            Heading = 300,
            EngineOnePower = 90,
            EngineTwoPower = 90,
            FuelPercentage = 48,
            AlertMessage = $"{aircraftName}: FUEL LEAK DETECTED - FUEL QUANTITY DECREASING"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Maintain aircraft control and monitor fuel", CorrectAction = "Monitor Fuel", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Identify affected fuel system", CorrectAction = "Identify Fuel Leak", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Isolate affected fuel source if required", CorrectAction = "Fuel Cutoff", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Divert to nearest suitable airport", CorrectAction = "Prepare Diversion", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Monitor Fuel")
        {
            state.FuelPercentage = Math.Max(0, state.FuelPercentage - 5);
        }

        if (actionName == "Identify Fuel Leak")
        {
            state.AlertMessage = "LEFT FUEL SYSTEM LEAK SUSPECTED";
        }

        if (actionName == "Fuel Cutoff")
        {
            state.FuelCutoff = true;
            state.AlertMessage = "AFFECTED FUEL SYSTEM ISOLATED";
        }

        if (actionName == "Prepare Diversion")
        {
            state.AlertMessage = "DIVERSION PLANNED - LAND AS SOON AS PRACTICAL";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}