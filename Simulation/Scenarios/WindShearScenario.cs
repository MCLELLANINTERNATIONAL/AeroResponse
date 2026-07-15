using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Scenarios;

public class WindShearScenario : ISimulationScenario
{
    public int ScenarioId => 10;

    public string ScenarioType => "Wind Shear";

    public CockpitState Start(CockpitLayoutDefinition aircraft)
    {
        var defaults = aircraft.DefaultState;
        var engines = Enumerable.Range(1, aircraft.EngineCount)
            .Select(number => new EngineState
            {
                Number = number,
                Power = Math.Max(0, defaults.NormalEnginePower - 5),
                Running = true,
                FuelPercentage = defaults.FuelPercentage
            })
            .ToList();

        return new CockpitState
        {
            Airspeed = defaults.CruiseAirspeed + 30,
            Altitude = defaults.CruiseAltitude * 0.30,
            Heading = defaults.DefaultHeading,
            VerticalSpeed = -1200,
            DisplayedVerticalSpeed = -1200,
            Pitch = defaults.DefaultPitch,
            Bank = defaults.DefaultBank,
            FuelPercentage = defaults.FuelPercentage,
            Engines = engines,
            AlertMessage = $"{aircraft.Name}: WINDSHEAR WARNING - TAKEOFF/LANDING PHASE"
        };
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(CockpitLayoutDefinition aircraft, int scenarioId)
    {
        return
        [
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 1, Instruction = "Apply maximum thrust", CorrectAction = "Maximum Thrust", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 2, Instruction = "Maintain pitch attitude", CorrectAction = "Maintain Pitch", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 3, Instruction = "Do not change configuration until clear", CorrectAction = "Hold Configuration", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 4, Instruction = "Monitor vertical speed and altitude", CorrectAction = "Monitor Flight Path", IsSafetyCritical = true },
            new() { EmergencyScenarioId = scenarioId, AircraftType = aircraft.Name, StepOrder = 5, Instruction = "Advise ATC when able", CorrectAction = "Declare Emergency", IsSafetyCritical = false }
        ];
    }

    public CockpitState ApplyPilotAction(CockpitState state, string actionName)
    {
        if (actionName == "Maximum Thrust")
        {
            foreach (var engine in state.Engines)
            {
                engine.Power = 100;
            }
            state.Airspeed += 20;
        }

        if (actionName == "Maintain Pitch")
        {
            state.VerticalSpeed = 800;
        }

        if (actionName == "Hold Configuration")
        {
            state.AlertMessage = "CONFIGURATION HELD - RECOVERING FROM WINDSHEAR";
        }

        if (actionName == "Monitor Flight Path")
        {
            state.Altitude += 500;
            state.AlertMessage = "POSITIVE CLIMB - WINDSHEAR RECOVERY IN PROGRESS";
        }

        return state;
    }

    public bool IsActionCorrect(CockpitLayoutDefinition aircraft, string actionName, int expectedStep)
    {
        var steps = GetProcedureSteps(aircraft, 0);
        return steps.Any(s => s.StepOrder == expectedStep && s.CorrectAction == actionName);
    }
}