using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class CabinDepressurizationScenario : ISimulationScenario
{
    public int ScenarioId => 2;

    public string ScenarioType => "Cabin Depressurization";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 300,
            Altitude = 35000,
            Heading = 90,
            VerticalSpeed = -3000,
            AlertMessage = $"{aircraft.Name}: CABIN PRESSURE WARNING"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Don oxygen masks", CorrectAction = "Oxygen Masks", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Begin emergency descent", CorrectAction = "Emergency Descent", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Set transponder emergency code", CorrectAction = "Set Emergency Code", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Level at safe altitude", CorrectAction = "Level Off", IsSafetyCritical = true }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Emergency Descent")
        {
            state.VerticalSpeed = -5000;
            state.Altitude -= 5000;
        }

        if (actionName == "Level Off")
        {
            state.VerticalSpeed = 0;
            state.AlertMessage = "AIRCRAFT LEVELLED AT SAFE ALTITUDE";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}