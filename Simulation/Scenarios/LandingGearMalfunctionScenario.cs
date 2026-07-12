using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class LandingGearMalfunctionScenario : ISimulationScenario
{
    public string ScenarioType => "Landing Gear Malfunction";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 190,
            Altitude = 4000,
            Heading = 310,
            AlertMessage = $"{aircraftName}: LANDING GEAR UNSAFE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Go around if unstable", CorrectAction = "Go Around", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Check landing gear indication", CorrectAction = "Check Gear Status", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Attempt alternate gear extension", CorrectAction = "Alternate Gear Extension", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Prepare emergency landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Alternate Gear Extension")
        {
            state.AlertMessage = "ALTERNATE GEAR EXTENSION ATTEMPTED";
        }

        return state;
    }

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}