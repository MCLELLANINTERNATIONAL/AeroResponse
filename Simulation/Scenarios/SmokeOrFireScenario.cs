using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class SmokeOrFireScenario : ISimulationScenario
{
    public int ScenarioId => 9;

    public string ScenarioType => "Smoke or Fire";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 1_500,
            MinimumAirspeed = 70,
            RequiresAircraftAirborne = true,
            RequiresEnginesRunning = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);

        currentState.FireDetected = true;

        currentState.AlertMessage =
            $"{aircraft.Name}: SMOKE OR FIRE WARNING - " +
            "CABIN/COCKPIT SOURCE UNKNOWN";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        return
        [
            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,
                Instruction = "Don oxygen masks",
                CorrectAction = "Oxygen Masks",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction = "Identify the smoke or fire source. (Interact with the fire detection Readout Light)",
                CorrectAction = "Identify Smoke Source",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction =
                    "Isolate the affected system by activating cabin fire suppression",
                CorrectAction = "Activate Fire Suppression",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction = "Declare emergency with air traffic control. (Satellite Power On, Satelite Connect, Declare Emergency)",
                CorrectAction = "Declare Emergency",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
                Instruction = "Prepare for an immediate landing. (Reduce pitch and vertical speed to a descending position)",
                CorrectAction = "Prepare Landing",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            }
        ];
    }

    public CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName)
    {
        ArgumentNullException.ThrowIfNull(state);

        switch (actionName)
        {
            case "Oxygen Masks":
                state.AlertMessage =
                    "CREW OXYGEN ACTIVE - CONTINUE SMOKE/FIRE CHECKLIST";
                break;

            case "Identify Smoke Source":
                state.AlertMessage =
                    "SMOKE SOURCE IDENTIFIED - " +
                    "AFFECTED SYSTEM REQUIRES ISOLATION";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - " +
                    "SMOKE/FIRE IMMEDIATE LANDING REQUIRED";
                break;
        }

        return state;
    }

    public bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep)
    {
        return GetProcedureSteps(
                aircraft,
                scenarioId: 0)
            .Any(step =>
                step.StepOrder == expectedStep &&
                step.ValidationType ==
                    ProcedureValidationType.PilotAction &&
                string.Equals(
                    step.CorrectAction,
                    actionName,
                    StringComparison.OrdinalIgnoreCase));
    }

    public bool IsStepSatisfied(
        CockpitState state,
        int stepOrder)
    {
        ArgumentNullException.ThrowIfNull(state);

        return stepOrder switch
        {
            3 =>
                state.FireSuppressionActivated,

            5 =>
                state.FlightPhase is
                    "Descent" or
                    "Approach" or
                    "Landing",

            _ => false
        };
    }
}