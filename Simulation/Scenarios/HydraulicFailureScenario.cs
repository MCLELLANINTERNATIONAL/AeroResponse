using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class HydraulicFailureScenario : ISimulationScenario
{
    public string ScenarioType => "Hydraulic Failure";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 250,
            Altitude = 18000,
            Heading = 120,
            AlertMessage = $"{aircraftName}: HYDRAULIC SYSTEM FAILURE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Identify failed hydraulic system", CorrectAction = "Identify Failure", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Activate alternate system", CorrectAction = "Activate Backup System", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Prepare abnormal landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Activate Backup System")
        {
            state.AlertMessage = "BACKUP HYDRAULIC SYSTEM ACTIVE";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}