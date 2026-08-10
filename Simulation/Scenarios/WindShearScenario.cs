using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class WindShearScenario : ISimulationScenario
{
    public int ScenarioId => 10;

    public string ScenarioType => "Wind Shear";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 300,
            MaximumAltitude = 2_500,
            MinimumAirspeed = 70,
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

        currentState.VerticalSpeed =
            Math.Min(
                currentState.VerticalSpeed,
                -1200);

        currentState.DisplayedVerticalSpeed =
            currentState.VerticalSpeed;

        currentState.Airspeed =
            Math.Max(
                0,
                currentState.Airspeed - 20);

        currentState.AlertMessage =
            $"{aircraft.Name}: WINDSHEAR WARNING - " +
            "IMMEDIATE ESCAPE MANEUVER REQUIRED";

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
                Instruction = "Apply maximum available thrust. (All Engine Power above 95%)",
                CorrectAction = "Maximum Thrust",
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
                    "Establish and maintain the windshear escape pitch. (A pitch between 10 and 18 degrees)",
                CorrectAction = "Maintain Pitch",
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
                    "Maintain the current aircraft configuration until clear. (Have Engine Power greater than 95% and maintain a pitch between 10 and 18 degrees.)",
                CorrectAction = "Hold Configuration",
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
                    "Monitor vertical speed and altitude trend. (Maintain a Verticle Speed Greater than 300 while airborne)",
                CorrectAction = "Monitor Flight Path",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = true
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
                Instruction = "Declare emergency with air traffic control. (Satellite Power On, Satelite Connect, Declare Emergency)",
                CorrectAction = "Declare Emergency",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = false
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
                    "ATC ADVISED - WINDSHEAR ENCOUNTER";
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
                state.Engines.Count > 0 &&
                state.Engines.All(
                    engine => engine.Power >= 95),

            2 =>
                state.Pitch >= 10 &&
                state.Pitch <= 18,
            
            3 =>
                state.Engines.Count > 0 &&
                state.Engines.All(
                    engine => engine.Power >= 95) &&
                state.Pitch >= 10 &&
                state.Pitch <= 18,

            4 =>
                state.VerticalSpeed >= 300 &&
                state.Altitude > 0,

            _ => false
        };
    }
}