using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFailureScenario : ISimulationScenario
{
    public string ScenarioType => "Engine Failure";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 240,
            Altitude = 10000,
            Heading = 180,
            EngineOnePower = 90,
            EngineTwoPower = 0,
            AlertMessage = $"{aircraftName}: ENGINE 2 FAILURE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Reduce affected engine thrust", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Shut down failed engine", CorrectAction = "Engine Shutdown", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Prepare single-engine landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Stabilize Aircraft") state.VerticalSpeed = 0;
        if (actionName == "Reduce Throttle") state.EngineTwoPower = 0;
        if (actionName == "Declare Emergency") state.AlertMessage = "EMERGENCY DECLARED - SINGLE ENGINE PROCEDURE ACTIVE";
        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}