using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFailureScenario : ISimulationScenario
{
    public int ScenarioId => 4;

    public string ScenarioType => "Engine Failure";

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

        var failedEngineNumber = aircraft.EngineCount >= 2 ? 2 : aircraft.EngineCount;
        if (failedEngineNumber > 0)
        {
            var failedEngine = engines.FirstOrDefault(e => e.Number == failedEngineNumber);
            if (failedEngine is not null)
            {
                failedEngine.Power = 0;
                failedEngine.Running = false;
            }
        }

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
            AlertMessage = $"{aircraft.Name}: ENGINE {failedEngineNumber} FAILURE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Reduce affected engine thrust", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Shut down failed engine", CorrectAction = "Engine Shutdown", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Prepare single-engine landing", CorrectAction = "Prepare Landing", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Stabilize Aircraft") state.VerticalSpeed = 0;
        if (actionName == "Reduce Throttle")
        {
            var engine = state.Engines.FirstOrDefault(e => e.Number == 2);
            if (engine is not null) engine.Power = 0;
        }
        if (actionName == "Declare Emergency") state.AlertMessage = "EMERGENCY DECLARED - SINGLE ENGINE PROCEDURE ACTIVE";
        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}