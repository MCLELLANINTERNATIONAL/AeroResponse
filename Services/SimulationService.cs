using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Services;

public class SimulationService
{
    private readonly SimulationEngine _simulationEngine = new();
    private readonly PerformanceScoringEngine _scoringEngine = new();

    private ScenarioRun? _currentRun;
    private CockpitState? _currentState;
    private CockpitLayoutDefinition? _currentAircraft;
    private string? _currentScenarioType;

    private List<ScenarioProcedureStep> _expectedSteps = [];
    private readonly List<PilotAction> _pilotActions = [];

    public CockpitState StartSimulation(
        string userId,
        int aircraftId,
        int scenarioId,
        CockpitLayoutDefinition aircraft,
        string scenarioType)
    {
        _currentAircraft = aircraft;
        _currentScenarioType = scenarioType;

        _currentRun = new ScenarioRun
        {
            UserId = userId,
            AircraftId = aircraftId,
            EmergencyScenarioId = scenarioId,
            AircraftName = aircraft.Name,
            ScenarioName = scenarioType
        };

        _expectedSteps = _simulationEngine.GetProcedureSteps(
            scenarioType,
            aircraft,
            scenarioId);

        _currentState = _simulationEngine.StartScenario(
            scenarioType,
            aircraft);

        _pilotActions.Clear();

        return _currentState;
    }

    public CockpitState SubmitPilotAction(string actionName)
    {
        if (_currentRun is null ||
            _currentState is null ||
            _currentAircraft is null ||
            string.IsNullOrWhiteSpace(_currentScenarioType))
        {
            throw new InvalidOperationException("No active simulation.");
        }

        var nextStep = _pilotActions.Count + 1;

        var expectedStep = _expectedSteps.FirstOrDefault(
            step => step.StepOrder == nextStep);

        var correctAction = _simulationEngine.IsActionCorrect(
            _currentScenarioType,
            _currentAircraft,
            actionName,
            nextStep);

        _pilotActions.Add(new PilotAction
        {
            ScenarioRunId = _currentRun.Id,
            ActionName = actionName,
            StepOrder = nextStep,
            WasCorrect = correctAction,
            WasInCorrectOrder = correctAction,
            IsSafetyCritical = expectedStep?.IsSafetyCritical ?? false
        });

        _currentState = _simulationEngine.ApplyAction(
            _currentScenarioType,
            _currentState,
            actionName);

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

        return _scoringEngine.GenerateReport(
            _currentRun,
            _pilotActions,
            _expectedSteps);
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