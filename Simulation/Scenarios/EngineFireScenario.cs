using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFireScenario : ISimulationScenario
{
    public int ScenarioId => 1;

    public string ScenarioType => "Engine Fire";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 2_000,
            MinimumAirspeed = 80,
            MinimumAverageEnginePower = 50,
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
                $"{aircraft.Name} cannot run the engine-fire scenario " +
                "because no engines are available.");
        }

        var affectedEngine =
            currentState.Engines.FirstOrDefault(
                engine => engine.Number == 2)
            ?? currentState.Engines[0];

        affectedEngine.Power =
            Math.Min(affectedEngine.Power, 40);

        affectedEngine.Running = true;
        affectedEngine.OnFire = true;
        affectedEngine.EngineFire = true;
        affectedEngine.FuelCutoff = false;
        affectedEngine.FireSuppressionActivated = false;

        // Give the pilot an actual handling disturbance.
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
            $"{aircraft.Name}: ENGINE {affectedEngine.Number} FIRE DETECTED";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        var affectedEngineNumber =
            aircraft.EngineCount >= 2 ? 2 : 1;

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
                    $"Reduce engine {affectedEngineNumber} thrust",
                CorrectAction =
                    $"Reduce Throttle Engine {affectedEngineNumber}",
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
                    $"Cut off fuel to engine {affectedEngineNumber}",
                CorrectAction =
                    $"Cut Fuel Engine {affectedEngineNumber}",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction =
                    $"Activate fire suppression for engine {affectedEngineNumber}",
                CorrectAction =
                    $"Activate Fire Suppression Engine {affectedEngineNumber}",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
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
                StepOrder = 6,
                Instruction = "Prepare to divert and land",
                CorrectAction = "Prepare Landing",
                ValidationType =
                    ProcedureValidationType.PilotAction,
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
            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - PREPARE TO DIVERT";
                break;

            case "Prepare Landing":
                state.AlertMessage =
                    "DIVERSION AND LANDING PREPARATION ACTIVE";
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
                ScenarioId)
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

        var affectedEngineNumber =
            state.Engines.Count >= 2 ? 2 : 1;

        var affectedEngine =
            state.Engines.FirstOrDefault(
                engine =>
                    engine.Number ==
                    affectedEngineNumber);

        if (affectedEngine is null)
        {
            return false;
        }

        return stepOrder switch
        {
            1 =>
                Math.Abs(state.Bank) <= 5 &&
                Math.Abs(state.VerticalSpeed) <= 300 &&
                Math.Abs(state.Pitch) <= 5,

            2 =>
                affectedEngine.Power <= 20,

            3 =>
                affectedEngine.FuelCutoff &&
                affectedEngine.Power <= 0 &&
                !affectedEngine.Running,

            4 =>
                affectedEngine.FireSuppressionActivated,

            _ => false
        };
    }
}