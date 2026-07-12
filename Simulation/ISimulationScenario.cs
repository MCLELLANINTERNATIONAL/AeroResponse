using AeroResponse.Models;

namespace AeroResponse.Simulation;

public interface ISimulationScenario
{
    string ScenarioType { get; }

    CockpitState Start(string aircraftName);

    List<ScenarioProcedureStep> GetProcedureSteps(string aircraftName, int scenarioId);

    CockpitState ApplyPilotAction(CockpitState state, string actionName);

    bool IsActionCorrect(string actionName, int expectedStep);
}