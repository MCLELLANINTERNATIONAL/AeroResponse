using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class ElectricalFailureScenario : ISimulationScenario
{
    public int ScenarioId => 3;

    public string ScenarioType => "Electrical Failure";

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
            Airspeed = defaults.CruiseAirspeed * 0.80,
            Altitude = defaults.CruiseAltitude,
            Heading = defaults.DefaultHeading,
            VerticalSpeed = defaults.DefaultVerticalSpeed,
            DisplayedVerticalSpeed = defaults.DefaultVerticalSpeed,

            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,

            FuelPercentage = defaults.FuelPercentage,
            Engines = engines,
            AlertMessage = $"{aircraft.Name}: ELECTRICAL SYSTEM FAILURE - BACKUP POWER REQUIRED"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Maintain aircraft control", CorrectAction = "Stabilize Aircraft", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Activate backup electrical power", CorrectAction = "Activate Backup Power", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Reduce non-essential electrical load", CorrectAction = "Reduce Electrical Load", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Declare emergency", CorrectAction = "Declare Emergency", IsSafetyCritical = false },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Prepare diversion using available systems", CorrectAction = "Prepare Diversion", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Activate Backup Power")
        {
            state.AlertMessage = "BACKUP POWER ONLINE - ESSENTIAL SYSTEMS RESTORED";
        }

        if (actionName == "Reduce Electrical Load")
        {
            state.AlertMessage = "NON-ESSENTIAL ELECTRICAL LOAD REDUCED";
        }

        if (actionName == "Declare Emergency")
        {
            state.AlertMessage = "EMERGENCY DECLARED - DIVERSION REQUIRED";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}