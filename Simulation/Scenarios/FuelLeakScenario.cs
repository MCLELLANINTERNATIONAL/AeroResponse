using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class FuelLeakScenario : ISimulationScenario
{
    public int ScenarioId => 6;

    public string ScenarioType => "Fuel Leak";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 1_000,
            MinimumAirspeed = 60,
            MinimumFuelPercentage = 25,
            RequiresAircraftAirborne = true,
            RequiresEnginesRunning = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);

        if (currentState.FuelTanks.Count == 0)
        {
            throw new InvalidOperationException(
                $"{aircraft.Name} cannot run the fuel-leak scenario " +
                "because no fuel tanks are configured.");
        }

        var affectedTank =
            currentState.FuelTanks.First();

        currentState.FuelLeakActive = true;
        currentState.LeakingFuelTankNumber =
            affectedTank.Number;

        currentState.AlertMessage =
            $"{aircraft.Name}: FUEL LEAK DETECTED - " +
            $"TANK {affectedTank.Number} QUANTITY DECREASING";

        return currentState;
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        const int affectedTankNumber = 1;

        return
        [
            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,
                Instruction =
                    "Maintain aircraft control and monitor fuel quantity",
                CorrectAction = "Monitor Fuel",
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
                    $"Identify tank {affectedTankNumber} as the leaking source",
                CorrectAction =
                    $"Identify Fuel Leak Tank {affectedTankNumber}",
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
                    $"Isolate fuel tank {affectedTankNumber}",
                CorrectAction =
                    $"Isolate Fuel Tank {affectedTankNumber}",
                ValidationType =
                    ProcedureValidationType.PilotAction,
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
                    "Divert to the nearest suitable airport",
                CorrectAction = "Prepare Diversion",
                ValidationType =
                    ProcedureValidationType.CockpitState,
                IsSafetyCritical = false
            }
        ];
    }

    public CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName)
    {
        ArgumentNullException.ThrowIfNull(state);

        var affectedTank =
            state.FuelTanks.FirstOrDefault(
                tank =>
                    tank.Number ==
                    state.LeakingFuelTankNumber);

        switch (actionName)
        {
            case "Monitor Fuel":
                state.AlertMessage =
                    affectedTank is null
                        ? "FUEL QUANTITY MONITORING ACTIVE"
                        : $"TANK {affectedTank.Number} FUEL LOSS CONFIRMED";
                break;

            case "Isolate Fuel Tank 1":
                state.FuelLeakActive = false;

                state.AlertMessage =
                    "AFFECTED FUEL SOURCE ISOLATED";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - FUEL LEAK DIVERSION REQUIRED";
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

        var affectedTank =
            state.FuelTanks.FirstOrDefault(
                tank =>
                    tank.Number ==
                    state.LeakingFuelTankNumber);

        return stepOrder switch
        {
            3 =>
                affectedTank is not null &&
                affectedTank.Isolated &&
                !state.FuelLeakActive,

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