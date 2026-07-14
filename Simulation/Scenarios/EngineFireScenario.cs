using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFireScenario : ISimulationScenario
{
    public int ScenarioId => 5;

    public string ScenarioType => "Engine Fire";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        return new CockpitState
        {
            Airspeed = 260,
            Altitude = 8000,
            Heading = 270,
            Engines =
            [
                new EngineState { Number = 1, Power = 92, Running = true },
                new EngineState { Number = 2, Power = 40, Running = true, EngineFire = true }
            ],
            AlertMessage = $"{aircraft.Name}: ENGINE 2 FIRE DETECTED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Reduce affected engine thrust", CorrectAction = "Reduce Throttle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Cut off fuel to affected engine", CorrectAction = "Fuel Cutoff", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Shut down affected engine", CorrectAction = "Engine Shutdown", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Pull fire handle", CorrectAction = "Pull Fire Handle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Discharge fire bottle", CorrectAction = "Discharge Fire Bottle", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 6, Instruction = "Declare emergency and prepare diversion", CorrectAction = "Declare Emergency", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Reduce Throttle")
        {
            var engine = state.Engines.FirstOrDefault(e => e.Number == 2);
            if (engine is not null) engine.Power = 20;
        }
        if (actionName == "Fuel Cutoff")
        {
            var engine = state.Engines.FirstOrDefault(e => e.Number == 2);
            if (engine is not null) engine.FuelCutoff = true;
        }
        if (actionName == "Discharge Fire Bottle")
        {
            var engine = state.Engines.FirstOrDefault(e => e.Number == 2);
            if (engine is not null) engine.FireSuppressionActivated = true;
        }
        var affectedEngine = state.Engines.FirstOrDefault(e => e.Number == 2);
        if (affectedEngine is not null && affectedEngine.FuelCutoff && affectedEngine.FireSuppressionActivated)
        {
            affectedEngine.EngineFire = false;
            state.AlertMessage = "ENGINE FIRE SUPPRESSED - DIVERT TO NEAREST AIRPORT";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}