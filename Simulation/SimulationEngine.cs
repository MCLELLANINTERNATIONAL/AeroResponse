using System.Reflection;
using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;
using AeroResponse.Simulation.Scenarios;

namespace AeroResponse.Simulation;

public class SimulationEngine
{
    private readonly List<ISimulationScenario> _scenarios;

    public SimulationEngine()
    {
        _scenarios = DiscoverScenarios();
    }

    private static List<ISimulationScenario> DiscoverScenarios()
    {
        var scenarioType = typeof(ISimulationScenario);

        return scenarioType.Assembly
            .GetTypes()
            .Where(type =>
                scenarioType.IsAssignableFrom(type) &&
                type is { IsInterface: false, IsAbstract: false } &&
                type.Namespace == "AeroResponse.Simulation.Scenarios" &&
                type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type =>
                (ISimulationScenario)Activator.CreateInstance(type)!)
            .ToList();
    }

    public ISimulationScenario GetScenario(string scenarioType)
    {
        return _scenarios.FirstOrDefault(s =>
            s.ScenarioType.Equals(
                scenarioType,
                StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Scenario '{scenarioType}' not found.");
    }

    public IReadOnlyList<ISimulationScenario> GetScenarios()
    {
        return _scenarios
            .OrderBy(s => s.ScenarioType)
            .ToList();
    }

    public CockpitState StartScenario(
        string scenarioType,
        CockpitLayoutDefinition aircraft)
    {
        return GetScenario(scenarioType)
            .Start(aircraft);
    }

    public CockpitState ApplyAction(
        string scenarioType,
        CockpitState state,
        string actionName)
    {
        return GetScenario(scenarioType)
            .ApplyPilotAction(state, actionName);
    }

    public List<ScenarioProcedureStep> GetProcedureSteps(
        string scenarioType,
        CockpitLayoutDefinition aircraft,
        int scenarioId)
    {
        return GetScenario(scenarioType)
            .GetProcedureSteps(aircraft, scenarioId);
    }

    public bool IsActionCorrect(
        string scenarioType,
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep)
    {
        return GetScenario(scenarioType)
            .IsActionCorrect(
                aircraft,
                actionName,
                expectedStep);
    }
}