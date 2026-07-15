using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class CabinDepressurizationScenario : ISimulationScenario
{
    public int ScenarioId => 2;

    public string ScenarioType => "Cabin Depressurization";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        var defaults = aircraft.DefaultState;
        var engines = Enumerable.Range(1, aircraft.EngineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = defaults.NormalEnginePower,
                Running = true,
                FuelPercentage = defaults.FuelPercentage
            })
            .ToList();

        return new CockpitState
        {
            Airspeed = defaults.CruiseAirspeed,
            Altitude = defaults.CruiseAltitude,
            Heading = defaults.DefaultHeading,
            VerticalSpeed = defaults.DefaultVerticalSpeed,
            DisplayedVerticalSpeed = defaults.DefaultVerticalSpeed,

            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,

            FuelPercentage = defaults.FuelPercentage,
            Engines = engines,
            AlertMessage = $"{aircraft.Name}: CABIN PRESSURE WARNING"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Don oxygen masks", CorrectAction = "Oxygen Masks", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Begin emergency descent", CorrectAction = "Emergency Descent", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Set transponder emergency code", CorrectAction = "Set Emergency Code", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Level at safe altitude", CorrectAction = "Level Off", IsSafetyCritical = true }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Emergency Descent")
        {
            state.VerticalSpeed = -5000;
            state.Altitude -= 5000;
        }

        if (actionName == "Level Off")
        {
            state.VerticalSpeed = 0;
            state.AlertMessage = "AIRCRAFT LEVELLED AT SAFE ALTITUDE";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}