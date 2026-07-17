using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class LandingGearMalfunctionScenario : ISimulationScenario
{
    public int ScenarioId => 8;

    public string ScenarioType => "Landing Gear Malfunction";

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
            AlertMessage = $"{aircraft.Name}: LANDING GEAR UNSAFE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Go around if unstable", CorrectAction = "Go Around", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Check landing gear indication", CorrectAction = "Check Gear Status", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Attempt alternate gear extension", CorrectAction = "Alternate Gear Extension", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Prepare emergency landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Alternate Gear Extension")
        {
            state.AlertMessage = "ALTERNATE GEAR EXTENSION ATTEMPTED";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}