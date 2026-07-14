using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFailureScenario : ISimulationScenario
{
    public int ScenarioId => 4;

    public string ScenarioType => "Engine Failure";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 240,
            Altitude = 10000,
            Heading = 180,
            Engines =
            [
                new EngineState { Number = 1, Power = 90, Running = true },
                new EngineState { Number = 2, Power = 0, Running = false }
            ],
            AlertMessage = $"{aircraft.Name}: ENGINE 2 FAILURE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Reduce affected engine thrust", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Shut down failed engine", CorrectAction = "Engine Shutdown", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Prepare single-engine landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Stabilize Aircraft") state.VerticalSpeed = 0;
        if (actionName == "Reduce Throttle")
        {
            var engine = state.Engines.FirstOrDefault(e => e.Number == 2);
            if (engine is not null) engine.Power = 0;
        }
        if (actionName == "Declare Emergency") state.AlertMessage = "EMERGENCY DECLARED - SINGLE ENGINE PROCEDURE ACTIVE";
        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}