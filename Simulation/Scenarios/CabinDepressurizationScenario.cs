using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class CabinDepressurizationScenario : ISimulationScenario
{
    public int ScenarioId => 2;

    public string ScenarioType => "Cabin Depressurization";

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
        return
        [
            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,
                Instruction = "Don oxygen masks",
                CorrectAction = "Oxygen Masks",
                ValidationType = ProcedureValidationType.PilotAction,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction = "Begin emergency descent",
                CorrectAction = "Emergency Descent",
                ValidationType = ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction = "Transmit emergency status",
                CorrectAction = "Transmit Emergency",
                ValidationType = ProcedureValidationType.PilotAction,
                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction = "Declare emergency",
                CorrectAction = "Declare Emergency",
                ValidationType = ProcedureValidationType.PilotAction,
                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
                Instruction = "Level at safe altitude",
                CorrectAction = "Level Off",
                ValidationType = ProcedureValidationType.CockpitState,
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
                    "OXYGEN MASKS ON - BEGIN EMERGENCY DESCENT";
                break;

            case "Set Emergency Code":
                state.AlertMessage =
                    "EMERGENCY TRANSPONDER CODE SET";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - CABIN DEPRESSURIZATION";
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
            // Emergency descent has actually been established.
            2 =>
                state.VerticalSpeed <= -900,

            // Aircraft has reached a safer altitude and is substantially level.
            5 =>
                state.Altitude <= 10_000 &&
                Math.Abs(state.VerticalSpeed) <= 300,

            _ => false
        };
    }
}