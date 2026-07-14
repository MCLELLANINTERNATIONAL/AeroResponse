using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class WindShearScenario : ISimulationScenario
{
    public int ScenarioId => 10;

    public string ScenarioType => "Wind Shear";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 145,
            Altitude = 900,
            Heading = 270,
            VerticalSpeed = -1200,
            Engines =
            [
                new EngineState { Number = 1, Power = 70, Running = true },
                new EngineState { Number = 2, Power = 70, Running = true }
            ],
            AlertMessage = $"{aircraft.Name}: WINDSHEAR WARNING - TAKEOFF/LANDING PHASE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Apply maximum thrust", CorrectAction = "Maximum Thrust", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Maintain pitch attitude", CorrectAction = "Maintain Pitch", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Do not change configuration until clear", CorrectAction = "Hold Configuration", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Monitor vertical speed and altitude", CorrectAction = "Monitor Flight Path", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Advise ATC when able", CorrectAction = "Declare Emergency", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Maximum Thrust")
        {
            foreach (var engine in state.Engines)
            {
                engine.Power = 100;
            }
            state.Airspeed += 20;
        }

        if (actionName == "Maintain Pitch")
        {
            state.VerticalSpeed = 800;
        }

        if (actionName == "Hold Configuration")
        {
            state.AlertMessage = "CONFIGURATION HELD - RECOVERING FROM WINDSHEAR";
        }

        if (actionName == "Monitor Flight Path")
        {
            state.Altitude += 500;
            state.AlertMessage = "POSITIVE CLIMB - WINDSHEAR RECOVERY IN PROGRESS";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}