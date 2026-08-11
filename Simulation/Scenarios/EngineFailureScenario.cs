using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFailureScenario : ISimulationScenario
{
    public int ScenarioId => 4;

    public string ScenarioType => "Engine Failure";

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

        if (currentState.Engines.Count == 0)
        {
            throw new InvalidOperationException(
                $"{aircraft.Name} cannot run the engine-failure scenario " +
                "because no engines are available in the cockpit state.");
        }

        var failedEngineNumber =
            currentState.Engines.Count >= 2
                ? 2
                : 1;

        var failedEngine =
            currentState.Engines.FirstOrDefault(
                engine =>
                    engine.Number ==
                    failedEngineNumber);

        if (failedEngine is null)
        {
            throw new InvalidOperationException(
                $"Engine {failedEngineNumber} was not found.");
        }

        failedEngine.Power = 0;
        failedEngine.Running = false;

        // Give the pilot something real to recover from.
        currentState.Bank =
            Math.Clamp(
                currentState.Bank + 8,
                -30,
                30);

        currentState.VerticalSpeed =
            Math.Min(
                currentState.VerticalSpeed,
                -500);

        currentState.AlertMessage =
            $"{aircraft.Name}: ENGINE {failedEngineNumber} FAILURE";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        var failedEngineNumber =
            aircraft.EngineCount >= 2
                ? 2
                : 1;

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
                    aircraft.EngineCount >= 2
                        ? $"Shut down failed engine."
                        : "Secure the failed engine and fuel source",
                CorrectAction =
                    $"Engine Shutdown {failedEngineNumber}",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
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
                StepOrder = 4,
                Instruction =
                    aircraft.EngineCount >= 2
                        ? "Prepare for a single-engine landing"
                        : "Prepare for a forced landing",
                CorrectAction = "Prepare Landing",
                ValidationType =
                    aircraft.EngineCount >= 2
                        ? ProcedureValidationType.PilotAction
                        : ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            }
        ];
    }

    public CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName)
    {
        ArgumentNullException.ThrowIfNull(state);

        var failedEngine =
            state.Engines.FirstOrDefault(
                engine =>
                    !engine.Running ||
                    engine.Power <= 0)
            ?? state.Engines.FirstOrDefault(
                engine =>
                    engine.Number ==
                    (state.Engines.Count >= 2 ? 2 : 1));

        switch (actionName)
        {
            case "Confirm Engine Failure":
                if (failedEngine is not null)
                {
                    state.AlertMessage =
                        $"ENGINE {failedEngine.Number} FAILURE CONFIRMED";
                }
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - ENGINE FAILURE PROCEDURE ACTIVE";
                break;

            case "Prepare Landing":
                state.AlertMessage =
                    state.Engines.Count >= 2
                        ? "SINGLE-ENGINE LANDING PREPARATION ACTIVE"
                        : "FORCED-LANDING PREPARATION ACTIVE";
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

        var failedEngineNumber =
            state.Engines.Count >= 2
                ? 2
                : 1;

        var failedEngine =
            state.Engines.FirstOrDefault(
                engine =>
                    engine.Number ==
                    failedEngineNumber);

        return stepOrder switch
        {
            1 =>
                Math.Abs(state.Bank) <= 5 &&
                Math.Abs(state.Pitch) <= 8 &&
                state.Airspeed >= 55,

            2 =>
                failedEngine is not null &&
                failedEngine.Power <= 0 &&
                !failedEngine.Running &&
                failedEngine.FuelCutoff,
            
            4 =>
                state.FlightPhase is
                    "Descent" or
                    "Approach" or
                    "Landing",

            _ => false
        };
    }
}