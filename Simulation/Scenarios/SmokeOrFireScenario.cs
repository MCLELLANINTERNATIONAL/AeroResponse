using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class SmokeOrFireScenario : ISimulationScenario
{
    public string ScenarioType => "Smoke or Fire";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 250,
            Altitude = 12000,
            Heading = 90,
            EngineOnePower = 85,
            EngineTwoPower = 85,
            AlertMessage = $"{aircraftName}: SMOKE OR FIRE WARNING - CABIN/COCKPIT"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Don oxygen masks", CorrectAction = "Oxygen Masks", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Identify smoke or fire source", CorrectAction = "Identify Smoke Source", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Activate appropriate fire suppression or isolation", CorrectAction = "Activate Fire Suppression", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Prepare immediate landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Oxygen Masks")
        {
            state.AlertMessage = "CREW OXYGEN ACTIVE - CONTINUE SMOKE/FIRE CHECKLIST";
        }

        if (actionName == "Identify Smoke Source")
        {
            state.AlertMessage = "SMOKE SOURCE IDENTIFICATION IN PROGRESS";
        }

        if (actionName == "Activate Fire Suppression")
        {
            state.FireSuppressionActivated = true;
            state.AlertMessage = "FIRE SUPPRESSION ACTIVE - LAND IMMEDIATELY";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}