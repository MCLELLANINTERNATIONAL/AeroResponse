using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFireScenario : ISimulationScenario
{
    public string ScenarioType => "Engine Fire";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 260,
            Altitude = 8000,
            Heading = 270,
            EngineOnePower = 92,
            EngineTwoPower = 40,
            EngineFire = true,
            AlertMessage = $"{aircraftName}: ENGINE 2 FIRE DETECTED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Reduce affected engine thrust", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Cut off fuel to affected engine", CorrectAction = "Fuel Cutoff", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Shut down affected engine", CorrectAction = "Engine Shutdown", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Pull fire handle", CorrectAction = "Pull Fire Handle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Discharge fire bottle", CorrectAction = "Discharge Fire Bottle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 6, Instruction = "Declare emergency and prepare diversion", CorrectAction = "Declare Emergency", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Reduce Throttle") state.EngineTwoPower = 20;
        if (actionName == "Fuel Cutoff") state.FuelCutoff = true;
        if (actionName == "Discharge Fire Bottle") state.FireSuppressionActivated = true;
        if (state.FuelCutoff && state.FireSuppressionActivated)
        {
            state.EngineFire = false;
            state.AlertMessage = "ENGINE FIRE SUPPRESSED - DIVERT TO NEAREST AIRPORT";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}