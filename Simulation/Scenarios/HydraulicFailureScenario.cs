using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class HydraulicFailureScenario : ISimulationScenario
{
    public int ScenarioId => 7;

    public string ScenarioType => "Hydraulic Failure";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 1_500,
            MinimumAirspeed = 70,
            MinimumHydraulicPressure = 2_500,
            RequiresAircraftAirborne = true,
            RequiresEnginesRunning = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);

        currentState.HydraulicPressure = 250;
        currentState.HydraulicPumpOnline = false;
        currentState.HydraulicFault = true;

        // Give the pilot something to recover from.
        currentState.Bank =
            Math.Clamp(
                currentState.Bank + 6,
                -30,
                30);

        currentState.VerticalSpeed =
            Math.Min(
                currentState.VerticalSpeed,
                -400);

        currentState.AlertMessage =
            $"{aircraft.Name}: HYDRAULIC SYSTEM FAILURE - " +
            "PRESSURE BELOW SAFE OPERATING RANGE";

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
                Instruction = "Maintain aircraft control",
                CorrectAction = "Stabilize Aircraft",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction =
                    "Confirm hydraulic pressure loss and identify the failed system",
                CorrectAction = "Identify Hydraulic Failure",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction = "Activate the alternate hydraulic system",
                CorrectAction = "Activate Backup Hydraulic System",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction = "Declare emergency",
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
                Instruction =
                    "Prepare for an abnormal landing and reduced braking capability",
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
            case "Identify Hydraulic Failure":
                state.AlertMessage =
                    $"HYDRAULIC PRESSURE LOW - " +
                    $"{state.HydraulicPressure:N0} PSI";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - " +
                    "ABNORMAL LANDING PROCEDURE REQUIRED";
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
            1 =>
                Math.Abs(state.Bank) <= 5 &&
                Math.Abs(state.VerticalSpeed) <= 300 &&
                Math.Abs(state.Pitch) <= 5,

            3 =>
                state.HydraulicPumpOnline &&
                state.HydraulicPressure >= 2_000 &&
                !state.HydraulicFault,

            5 =>
                state.FlightPhase is
                    "Descent" or
                    "Approach" or
                    "Landing" ||
                (state.Altitude <= 1_500 &&
                state.Airspeed < 90),

            _ => false
        };
    }
}