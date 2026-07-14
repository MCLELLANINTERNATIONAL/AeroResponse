using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class SmokeOrFireScenario : ISimulationScenario
{
    public int ScenarioId => 9;

    public string ScenarioType => "Smoke or Fire";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 250,
            Altitude = 12000,
            Heading = 90,
            Engines =
            [
                new EngineState { Number = 1, Power = 85, Running = true },
                new EngineState { Number = 2, Power = 85, Running = true }
            ],
            AlertMessage = $"{aircraft.Name}: SMOKE OR FIRE WARNING - CABIN/COCKPIT"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Don oxygen masks", CorrectAction = "Oxygen Masks", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Identify smoke or fire source", CorrectAction = "Identify Smoke Source", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Activate appropriate fire suppression or isolation", CorrectAction = "Activate Fire Suppression", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Prepare immediate landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
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
            foreach (var engine in state.Engines)
            {
                engine.FireSuppressionActivated = true;
            }
            state.AlertMessage = "FIRE SUPPRESSION ACTIVE - LAND IMMEDIATELY";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}