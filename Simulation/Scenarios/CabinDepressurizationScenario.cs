using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class CabinDepressurizationScenario
    : ISimulationScenario
{
    public int ScenarioId => 2;

    public string ScenarioType =>
        "Cabin Depressurization";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 10_000,
            MinimumAirspeed = 80,
            RequiresAircraftAirborne = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);

        currentState.AlertMessage =
            $"{aircraft.Name}: CABIN PRESSURE WARNING";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        return
        [
            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,

                Instruction =
                    "Don oxygen masks and confirm oxygen flow",

                CorrectAction =
                    "Oxygen Masks",

                ValidationType =
                    ProcedureValidationType.PilotAction,

                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,

                Instruction =
                    "Begin emergency descent",

                CorrectAction =
                    "Emergency Descent",

                ValidationType =
                    ProcedureValidationType.CockpitState,

                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,

                Instruction =
                    "Declare emergency with air traffic control",

                CorrectAction =
                    "Declare Emergency",

                ValidationType =
                    ProcedureValidationType.PilotAction,

                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,

                Instruction =
                    "Set transponder code 7700 if unable to " +
                    "establish immediate communication",

                CorrectAction =
                    "Set Emergency Code",

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
                    "Level at 10,000 feet or the minimum " +
                    "safe altitude, whichever is higher",

                CorrectAction =
                    "Level Off",

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
                    "CREW OXYGEN MASKS ON - " +
                    "OXYGEN FLOW CONFIRMED";
                break;

            case "Emergency Descent":
                state.VerticalSpeed = -5_000;
                state.Pitch = -10;

                state.AlertMessage =
                    "EMERGENCY DESCENT IN PROGRESS";
                break;

            case "Declare Emergency":
                state.CommunicationStatus =
                    "MAYDAY - CABIN DEPRESSURIZATION";

                state.AlertMessage =
                    "EMERGENCY DECLARED WITH " +
                    "AIR TRAFFIC CONTROL";
                break;

            case "Set Emergency Code":
                state.DynamicValues[
                    "communication.transponder-code"] = 7700;

                state.AlertMessage =
                    "TRANSPONDER SET TO 7700";
                break;

            case "Level Off":
                state.VerticalSpeed = 0;
                state.Pitch = 0;

                state.AlertMessage =
                    "AIRCRAFT LEVELLED AT SAFE ALTITUDE";
                break;
        }

        return state;
    }

    public bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep)
    {
        ArgumentNullException.ThrowIfNull(aircraft);

        return GetProcedureSteps(
                aircraft,
                scenarioId: 0)
            .Any(step =>
                step.StepOrder == expectedStep &&
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
            // Emergency descent has been established.
            2 =>
                state.VerticalSpeed <= -900,

            // Aircraft has reached a safer altitude
            // and is substantially level.
            5 =>
                state.Altitude <= 10_000 &&
                Math.Abs(state.VerticalSpeed) <= 300,

            _ => false
        };
    }
}