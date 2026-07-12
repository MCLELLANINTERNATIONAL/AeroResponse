using AeroResponse.Models;
using AeroResponse.Simulation.Scenarios;

namespace AeroResponse.Simulation;

public class SimulationEngine
{
    private readonly List<ISimulationScenario> _scenarios =
    [
        new EngineFireScenario(),
        new EngineFailureScenario(),
        new BirdStrikeScenario(),
        new CabinDepressurizationScenario(),
        new HydraulicFailureScenario(),
        new ElectricalFailureScenario(),
        new FuelLeakScenario(),
        new LandingGearMalfunctionScenario(),
        new SmokeOrFireScenario(),
        new WindShearScenario()
    ];

    public ISimulationScenario GetScenario(string scenarioType)
    {
        return _scenarios.FirstOrDefault(s =>
            s.ScenarioType.Equals(scenarioType, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Scenario '{scenarioType}' not found.");
    }

    public CockpitState StartScenario(string scenarioType, string aircraftName)
    {
        return GetScenario(scenarioType).Start(aircraftName);
    }

    public CockpitState ApplyAction(string scenarioType, CockpitState state, string actionName)
    {
        return GetScenario(scenarioType).ApplyPilotAction(state, actionName);
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(string scenarioType, string aircraftName, int scenarioId)
    {
        return GetScenario(scenarioType).GetProcedureSteps(aircraftName, scenarioId);
    }
}