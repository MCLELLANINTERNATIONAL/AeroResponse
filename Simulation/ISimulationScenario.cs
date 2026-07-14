using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation;

public interface ISimulationScenario
{
    int ScenarioId { get; }

    string ScenarioType { get; }

    CockpitState Start(
        CockpitLayoutDefinition aircraft);

    List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId);

    CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName);

    bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep);
}