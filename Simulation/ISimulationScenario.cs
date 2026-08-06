using AeroResponse.Models;
using AeroResponse.Simulation.Layouts;
using AeroResponse.Simulation.Scenarios;

namespace AeroResponse.Simulation;

public interface ISimulationScenario
{
    int ScenarioId { get; }

    string ScenarioType { get; }
    ScenarioStartCondition StartCondition { get; }

    CockpitState Start(
        CockpitLayoutDefinition aircraft,
        CockpitState currentState);

    List<ScenarioProcedureStep> GetProcedureSteps(
        CockpitLayoutDefinition aircraft,
        int scenarioId);

    bool IsStepSatisfied(
        CockpitState state,
        int stepOrder);

    CockpitState ApplyPilotAction(
        CockpitState state,
        string actionName);

    bool IsActionCorrect(
        CockpitLayoutDefinition aircraft,
        string actionName,
        int expectedStep);
}