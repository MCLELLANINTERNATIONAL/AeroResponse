using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class EngineFireScenario : ISimulationScenario
{
    public int ScenarioId => 1;

    public string ScenarioType => "Engine Fire";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        var engineCount = aircraft.EngineCount > 0
            ? aircraft.EngineCount
            : 2;

        var engines = Enumerable.Range(1, engineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = 92,
                Running = true,
                OnFire = false,
                EngineFire = false,
                FuelCutoff = false,
                FireSuppressionActivated = false
            })
            .ToList();

        // Engine 2 is the affected engine where available.
        var affectedEngine = engines.FirstOrDefault(engine => engine.Number == 2)
                             ?? engines.First();

        affectedEngine.Power = 40;
        affectedEngine.OnFire = true;
        affectedEngine.EngineFire = true;

        return new CockpitState
        {
            Airspeed = 260,
            Altitude = 8000,
            Heading = 270,
            Engines = engines,
            AlertMessage = $"{aircraft.Name}: ENGINE {affectedEngine.Number} FIRE DETECTED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        return
        [
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 1,
                Instruction = "Reduce affected engine thrust",
                CorrectAction = "Reduce Throttle",
                IsSafetyCritical = true
            },
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 2,
                Instruction = "Cut off fuel to affected engine",
                CorrectAction = "Fuel Cutoff",
                IsSafetyCritical = true
            },
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 3,
                Instruction = "Shut down affected engine",
                CorrectAction = "Engine Shutdown",
                IsSafetyCritical = true
            },
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 4,
                Instruction = "Pull fire handle",
                CorrectAction = "Pull Fire Handle",
                IsSafetyCritical = true
            },
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 5,
                Instruction = "Discharge fire bottle",
                CorrectAction = "Discharge Fire Bottle",
                IsSafetyCritical = true
            },
            new()
            {
                EmergencyScenarioId = scenarioId,
                AircraftType = aircraft.Name,
                StepOrder = 6,
                Instruction = "Declare emergency and prepare diversion",
                CorrectAction = "Declare Emergency",
                IsSafetyCritical = false
            }
        ];
    }

    public CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName)
    {
        var affectedEngine =
            state.Engines.FirstOrDefault(engine =>
                engine.EngineFire || engine.OnFire)
            ?? state.Engines.FirstOrDefault(engine => engine.Number == 2)
            ?? state.Engines.FirstOrDefault();

        if (affectedEngine is null)
        {
            state.AlertMessage = "No engine information is available.";
            return state;
        }

        switch (actionName)
        {
            case "Reduce Throttle":
                affectedEngine.Power = 20;
                break;

            case "Fuel Cutoff":
                affectedEngine.FuelCutoff = true;
                break;

            case "Engine Shutdown":
                affectedEngine.Power = 0;
                affectedEngine.Running = false;
                break;

            case "Pull Fire Handle":
                affectedEngine.FuelCutoff = true;
                break;

            case "Discharge Fire Bottle":
                affectedEngine.FireSuppressionActivated = true;
                break;

            case "Declare Emergency":
                state.AlertMessage =
                    "EMERGENCY DECLARED - PREPARE TO DIVERT";
                break;
        }

        if (affectedEngine.FuelCutoff &&
            affectedEngine.FireSuppressionActivated)
        {
            affectedEngine.EngineFire = false;
            affectedEngine.OnFire = false;

            state.AlertMessage =
                "ENGINE FIRE SUPPRESSED - DIVERT TO NEAREST AIRPORT";
        }

        return state;
    }

    public bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, ScenarioId);

        return steps.Any(step =>
            step.StepOrder == expectedStep &&
            string.Equals(
                step.CorrectAction,
                actionName,
                StringComparison.OrdinalIgnoreCase));
    }
}