using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class CabinDepressurizationScenario
    : ISimulationScenario
{
    public int ScenarioId => 2;

    public string ScenarioType =>
        "Cabin Depressurization";

    public CockpitState Start(
        CockpitLayoutDefinition aircraft)
    {
        if (aircraft.EngineCount < 1)
        {
            throw new InvalidOperationException(
                $"{aircraft.Name} cannot run the cabin " +
                "depressurization scenario because it " +
                "defines no engines.");
        }

        var defaults = aircraft.DefaultState;

        var engines =
            Enumerable.Range(1, aircraft.EngineCount)
                .Select(number => new EngineState
                {
                    Number = number,
                    Power = defaults.NormalEnginePower,
                    Running = true,
                    FuelPercentage =
                        defaults.FuelPercentage
                })
                .ToList();

        return new CockpitState
        {
            Airspeed = defaults.CruiseAirspeed,
            Altitude = defaults.CruiseAltitude,
            Heading = defaults.DefaultHeading,
            VerticalSpeed =
                defaults.DefaultVerticalSpeed,
            DisplayedVerticalSpeed =
                defaults.DefaultVerticalSpeed,

            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,

            FuelPercentage =
                defaults.FuelPercentage,

            Engines = engines,

            AlertMessage =
                $"{aircraft.Name}: CABIN PRESSURE WARNING"
        };
    }

    public List<ScenarioProcedureStep>
        GetProcedureSteps(
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
                    "Don oxygen masks and confirm oxygen flow",

                CorrectAction =
                    "Oxygen Masks",

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

                IsSafetyCritical = false
            },

            new ScenarioProcedureStep
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,

                Instruction =
                    "Set transponder code 7700 if unable " +
                    "to establish immediate communication",

                CorrectAction =
                    "Set Emergency Code",

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
            case "Oxygen Masks":
                state.AlertMessage =
                    "CREW OXYGEN MASKS ON - OXYGEN FLOW CONFIRMED";
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
                    "EMERGENCY DECLARED WITH AIR TRAFFIC CONTROL";
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
        var expectedProcedure =
            GetProcedureSteps(
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