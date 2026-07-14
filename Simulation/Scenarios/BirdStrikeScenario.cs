using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;
using AeroResponse.Simulation;

namespace AeroResponse.Simulation.Scenarios;

public class BirdStrikeScenario : ISimulationScenario
{
    public int ScenarioId => 1;
    private const int AffectedEngineNumber = 1;

    public string ScenarioType => "Bird Strike";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        if (aircraft.EngineCount < 1)
        {
            throw new InvalidOperationException(
                $"{aircraft.Name} cannot run the bird-strike scenario " +
                "because it defines no engines.");
        }

        var engines = Enumerable
            .Range(1, aircraft.EngineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = number == AffectedEngineNumber ? 65 : 90,
                Running = true,
                OnFire = false,
                FuelCutoff = false,
                FireSuppressionActivated = false
            })
            .ToList();

        return new CockpitState
        {
            Airspeed = 220,
            Altitude = 3000,
            Heading = 240,
            VerticalSpeed = 0,
            Engines = engines,

            AlertMessage =
                $"{aircraft.Name}: BIRD STRIKE - " +
                $"ENGINE {AffectedEngineNumber} PERFORMANCE DEGRADED"
        };
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
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction = engineAssessmentInstruction,
                CorrectAction = "Check Engine Status",
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction = throttleInstruction,
                CorrectAction = "Reduce Throttle",
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction = "Declare emergency",
                CorrectAction = "Declare Emergency",
                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
                Instruction = landingInstruction,
                CorrectAction = "Prepare Landing",
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
            case "Stabilize Aircraft":
                state.VerticalSpeed = 0;
                break;

            case "Check Engine Status":
                state.AlertMessage =
                    $"ENGINE {AffectedEngineNumber} DAMAGE CONFIRMED - " +
                    "MONITOR PARAMETERS";
                break;

            case "Reduce Throttle":
                var affectedEngine = state.Engines.FirstOrDefault(
                    engine => engine.Number == AffectedEngineNumber);

                if (affectedEngine is null)
                {
                    throw new InvalidOperationException(
                        $"Engine {AffectedEngineNumber} was not found " +
                        "in the current cockpit state.");
                }

                affectedEngine.Power = 45;
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - RETURN TO AIRPORT";
                break;

            case "Prepare Landing":
                state.AlertMessage =
                    "LANDING PREPARATION INITIATED";
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
}