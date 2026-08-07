using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class LandingGearMalfunctionScenario : ISimulationScenario
{
    public int ScenarioId => 8;

    public string ScenarioType =>
        "Landing Gear Malfunction";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 500,
            MaximumAltitude = 3_000,
            MinimumAirspeed = 60,
            RequiredFlightPhase = "Approach",
            RequiresAircraftAirborne = true,
            RequiresEnginesRunning = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);
        var affectedGear =
            currentState.LandingGears
                .FirstOrDefault(
                    gear =>
                        gear.Position ==
                        LandingGearPosition.RightMain)
            ?? currentState.LandingGears.FirstOrDefault();

        if (affectedGear is not null)
        {
            affectedGear.Status =
                LandingGearStatusValue.Unsafe;
        }

        currentState.AlertMessage =
            $"{aircraft.Name}: LANDING GEAR UNSAFE";

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
                Instruction =
                    "Go around if the approach is unstable",
                CorrectAction = "Go Around",
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
                    "Check the landing gear indications",
                CorrectAction = "Check Gear Status",
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
                    "Complete the aircraft alternate landing gear extension procedure",
                CorrectAction = "Alternate Gear Extension",
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
                    "Prepare for an emergency landing",
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
            case "Check Gear Status":
                state.AlertMessage =
                    "LANDING GEAR INDICATION CHECKED - " +
                    "ONE OR MORE UNITS REMAIN UNSAFE";
                break;

            case "Alternate Gear Extension":
                state.AlertMessage =
                    "ALTERNATE GEAR EXTENSION ATTEMPTED";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - " +
                    "LANDING GEAR MALFUNCTION";
                break;

            case "Prepare Landing":
                state.AlertMessage =
                    "EMERGENCY LANDING PREPARATION ACTIVE";
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
                state.VerticalSpeed >= 300 &&
                state.Pitch >= 5 &&
                state.Engines.Count > 0 &&
                state.Engines.Average(
                    engine => engine.Power) >= 80,
            3 =>
                state.AlternateGearExtensionActivated &&
                state.AlternateGearExtensionCompleted &&
                state.LandingGears.Count > 0 &&
                state.LandingGears.All(
                    gear =>
                        gear.Status ==
                        LandingGearStatusValue.DownAndLocked),

            _ => false
        };
    }
}