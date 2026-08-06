using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class BirdStrikeScenario : ISimulationScenario
{
    public int ScenarioId => 1;
    private const int AffectedEngineNumber = 1;

    public string ScenarioType => "Bird Strike";

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
        if (aircraft.EngineCount < 1)
        {
            throw new InvalidOperationException(
                $"{aircraft.Name} cannot run the bird-strike scenario " +
                "because it defines no engines.");
        }

        var affectedEngine =
            currentState.Engines.FirstOrDefault(
                engine =>
                    engine.Number ==
                    AffectedEngineNumber);

        if (affectedEngine is null)
        {
            throw new InvalidOperationException(
                $"Engine {AffectedEngineNumber} was not found.");
        }

        affectedEngine.Power =
            GetDegradedPower(
                affectedEngine.Power);
            
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
            $"{aircraft.Name}: BIRD STRIKE - " +
            $"ENGINE {AffectedEngineNumber} PERFORMANCE DEGRADED";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        var engineAssessmentInstruction =
            aircraft.EngineCount == 1
                ? "Assess engine performance"
                : $"Assess engine {AffectedEngineNumber} performance";

        var throttleInstruction =
            aircraft.EngineCount == 1
                ? "Reduce engine power if operation is unstable"
                : $"Reduce engine {AffectedEngineNumber} thrust if unstable";

        var landingInstruction =
            aircraft.EngineCount == 1
                ? "Prepare for an immediate return or forced landing"
                : "Return or divert for inspection";

        return
        [
            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,
                Instruction = "Maintain aircraft control",
                CorrectAction = "Stabilize Aircraft",
                ValidationType = ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction = engineAssessmentInstruction,
                CorrectAction = "Check Engine Status",
                ValidationType = ProcedureValidationType.PilotAction,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction = throttleInstruction,
                CorrectAction = "Reduce Throttle",
                ValidationType = ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
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
                Instruction = landingInstruction,
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
        switch (actionName)
        {

            case "Check Engine Status":
                state.AlertMessage =
                    $"ENGINE {AffectedEngineNumber} DAMAGE CONFIRMED - " +
                    "MONITOR PARAMETERS";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - RETURN TO AIRPORT";
                break;
        }

        return state;
    }

    public bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep)
    {
        var expectedProcedure = GetProcedureSteps(
                aircraft,
                scenarioId: 0)
            .FirstOrDefault(step =>
                step.StepOrder == expectedStep);

        return expectedProcedure is not null &&
               string.Equals(
                   expectedProcedure.CorrectAction,
                   actionName,
                   StringComparison.OrdinalIgnoreCase);
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
                state.Engines
                    .FirstOrDefault(
                        engine =>
                            engine.Number ==
                            AffectedEngineNumber)
                    ?.Power <= 45,
            5 =>
                state.VerticalSpeed <= -300 &&
                state.FlightPhase is
                    "Descent" or
                    "Approach" or
                    "Landing",
            _ => false
        };
    }

    private static int GetDegradedPower(double normalPower)
    {
        return Math.Max(
            0,
            (int)Math.Round(normalPower * 0.72));
    }
}