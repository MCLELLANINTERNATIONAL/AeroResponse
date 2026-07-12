using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class ElectricalFailureScenario : ISimulationScenario
{
    public string ScenarioType => "Electrical Failure";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 250,
            Altitude = 16000,
            Heading = 180,
            EngineOnePower = 88,
            EngineTwoPower = 88,
            AlertMessage = $"{aircraftName}: ELECTRICAL SYSTEM FAILURE - BACKUP POWER REQUIRED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Activate backup electrical power", CorrectAction = "Activate Backup Power", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Reduce non-essential electrical load", CorrectAction = "Reduce Electrical Load", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Prepare diversion using available systems", CorrectAction = "Prepare Diversion", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Activate Backup Power")
        {
            state.AlertMessage = "BACKUP POWER ONLINE - ESSENTIAL SYSTEMS RESTORED";
        }

        if (actionName == "Reduce Electrical Load")
        {
            state.AlertMessage = "NON-ESSENTIAL ELECTRICAL LOAD REDUCED";
        }

        if (actionName == "Declare Emergency")
        {
            state.AlertMessage = "EMERGENCY DECLARED - DIVERSION REQUIRED";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}