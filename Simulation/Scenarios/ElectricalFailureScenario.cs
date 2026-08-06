using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class ElectricalFailureScenario : ISimulationScenario
{
    public int ScenarioId => 3;

    public string ScenarioType => "Electrical Failure";

    public ScenarioStartCondition StartCondition =>
        new()
        {
            MinimumAltitude = 1_000,
            MinimumAirspeed = 60,
            RequiresAircraftAirborne = true,
            RequiresEnginesRunning = true,
            RequiresElectricalSystemOnline = true
        };

    public CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(currentState);

        // Primary generation system has failed.
        currentState.AlternatorOnline = false;
        currentState.ElectricalFault = true;

        // Battery temporarily carries the bus.
        currentState.BatteryOnline = true;
        currentState.BusVoltage = 24.0;
        currentState.BatteryVoltage = 23.5;

        // High electrical demand remains until load shedding.
        currentState.ElectricalLoadAmps =
            Math.Max(
                currentState.ElectricalLoadAmps,
                70);

        currentState.AlertMessage =
            $"{aircraft.Name}: ELECTRICAL SYSTEM FAILURE - " +
            "PRIMARY GENERATION OFFLINE";

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
                Instruction = "Activate backup electrical power",
                CorrectAction = "Activate Backup Power",
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
                    "Reduce non-essential electrical load",
                CorrectAction = "Reduce Electrical Load",
                ValidationType =
                    ProcedureValidationType.PilotAction,
                IsSafetyCritical = false
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
                    "Prepare diversion using available systems",
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

        switch (actionName)
        {
            case "Activate Backup Power":
                state.BatteryOnline = true;

                state.BusVoltage =
                    Math.Max(
                        state.BusVoltage,
                        24.0);

                state.BatteryVoltage =
                    Math.Max(
                        state.BatteryVoltage,
                        23.5);

                state.AlertMessage =
                    "BACKUP POWER ONLINE - " +
                    "ESSENTIAL SYSTEMS AVAILABLE";
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - DIVERSION REQUIRED";
                break;

            case "Prepare Diversion":
                state.AlertMessage =
                    "DIVERSION PREPARATION IN PROGRESS";
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
                state.ElectricalLoadAmps <= 30,
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