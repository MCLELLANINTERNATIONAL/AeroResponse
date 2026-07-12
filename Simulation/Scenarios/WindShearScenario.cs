using AeroResponse.Models;

namespace AeroResponse.Simulation.Scenarios;

public class WindShearScenario : ISimulationScenario
{
    public string ScenarioType => "Wind Shear";

    public CockpitState Start(string aircraftName)
    {
        return new CockpitState
        {
            Airspeed = 145,
            Altitude = 900,
            Heading = 270,
            VerticalSpeed = -1200,
            EngineOnePower = 70,
            EngineTwoPower = 70,
            AlertMessage = $"{aircraftName}: WINDSHEAR WARNING - TAKEOFF/LANDING PHASE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 1, Instruction = "Apply maximum thrust", CorrectAction = "Maximum Thrust", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 2, Instruction = "Maintain pitch attitude", CorrectAction = "Maintain Pitch", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 3, Instruction = "Do not change configuration until clear", CorrectAction = "Hold Configuration", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 4, Instruction = "Monitor vertical speed and altitude", CorrectAction = "Monitor Flight Path", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraftName, StepOrder = 5, Instruction = "Advise ATC when able", CorrectAction = "Declare Emergency", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Maximum Thrust")
        {
            state.EngineOnePower = 100;
            state.EngineTwoPower = 100;
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

    public bool IsActionCorrect(string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps("Generic Aircraft", 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}