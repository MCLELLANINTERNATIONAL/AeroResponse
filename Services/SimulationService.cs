using AeroResponse.Models;
using AeroResponse.Simulation;

namespace AeroResponse.Services;

public class SimulationService
{
    private readonly SimulationEngine _simulationEngine = new();
    private readonly PerformanceScoringEngine _scoringEngine = new();

    private ScenarioRun? _currentRun;
    private CockpitState? _currentState;
    private List<ScenarioProcedureStep> _expectedSteps = [];
    private readonly List<PilotAction> _pilotActions = [];

    public CockpitState StartSimulation(
        string userId,
        int aircraftId,
        int scenarioId,
        string aircraftName,
        string scenarioType)
    {
        _currentRun = new ScenarioRun
        {
            UserId = userId,
            AircraftId = aircraftId,
            EmergencyScenarioId = scenarioId,
            AircraftName = aircraftName,
            ScenarioName = scenarioType
        };

        _expectedSteps = _simulationEngine.GetProcedureSteps(scenarioType, aircraftName, scenarioId);
        _currentState = _simulationEngine.StartScenario(scenarioType, aircraftName);
        _pilotActions.Clear();

        return _currentState;
    }

    public CockpitState SubmitPilotAction(string scenarioType, string actionName)
    {
        if (_currentRun is null || _currentState is null)
        {
            throw new InvalidOperationException("No active simulation.");
        }

        var nextStep = _pilotActions.Count + 1;
        var expectedStep = _expectedSteps.FirstOrDefault(s => s.StepOrder == nextStep);
        var correctAction = expectedStep?.CorrectAction == actionName;

        _pilotActions.Add(new PilotAction
        {
            ScenarioRunId = _currentRun.Id,
            ActionName = actionName,
            StepOrder = nextStep,
            WasCorrect = correctAction,
            WasInCorrectOrder = correctAction,
            IsSafetyCritical = expectedStep?.IsSafetyCritical ?? false
        });

        _currentState = _simulationEngine.ApplyAction(scenarioType, _currentState, actionName);

        return _currentState;
    }

    public SimulationReport CompleteSimulation()
    {
        if (_currentRun is null)
        {
            throw new InvalidOperationException("No active simulation.");
        }

        _currentRun.CompletedAt = DateTime.UtcNow;
        _currentRun.Status = "Completed";

        return _scoringEngine.GenerateReport(_currentRun, _pilotActions, _expectedSteps);
    }

    public List<ScenarioProcedureStep> GetCurrentChecklist()
    {
        return _expectedSteps;
    }

    public List<PilotAction> GetPilotActions()
    {
        return _pilotActions;
    }
}