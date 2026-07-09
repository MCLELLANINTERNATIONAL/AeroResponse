using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class BirdStrikeScenario : ISimulationScenario
{
    public string ScenarioType => "Bird Strike";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 220,
            Altitude = 3000,
            Heading = 240,
            EngineOnePower = 65,
            EngineTwoPower = 90,
            AlertMessage = $"{aircraftName}: BIRD STRIKE - ENGINE 1 PERFORMANCE DEGRADED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Assess engine performance", CorrectAction = "Check Engine Status", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Reduce affected engine thrust if unstable", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Return or divert for inspection", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Stabilize Aircraft")
        {
            state.VerticalSpeed = 0;
        }

        if (actionName == "Check Engine Status")
        {
            state.AlertMessage = "ENGINE 1 DAMAGE CONFIRMED - MONITOR PARAMETERS";
        }

        if (actionName == "Reduce Throttle")
        {
            state.EngineOnePower = 45;
        }

        if (actionName == "Declare Emergency")
        {
            state.AlertMessage = "EMERGENCY DECLARED - RETURN TO AIRPORT";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}