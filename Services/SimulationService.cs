using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Services;

public class SimulationService(PerformanceDashboardService dashboardService)
{
    private readonly SimulationEngine _simulationEngine = new();
    private readonly PerformanceScoringEngine _scoringEngine = new();
    private ScenarioRun? _currentRun;
    private CockpitState? _currentState;
    private CockpitLayoutDefinition? _currentAircraft;
    private string? _currentScenarioType;
    private string _pilotName = "Pilot";
    private string _difficulty = "Intermediate";
    private List<ScenarioProcedureStep> _expectedSteps = [];
    private readonly List<PilotAction> _pilotActions = [];

    public CockpitState StartSimulation(string userId, int aircraftId, int scenarioId,
        CockpitLayoutDefinition aircraft, string scenarioType, string pilotName = "Pilot", string difficulty = "Intermediate")
    {
        _currentAircraft = aircraft;
        _currentScenarioType = scenarioType;
        _pilotName = pilotName;
        _difficulty = difficulty;
        _currentRun = new ScenarioRun
        {
            UserId = userId,
            AircraftId = aircraftId,
            EmergencyScenarioId = scenarioId,
            AircraftName = aircraft.Name,
            ScenarioName = scenarioType,
            StartedAt = DateTime.UtcNow
        };
        _expectedSteps = _simulationEngine.GetProcedureSteps(scenarioType, aircraft, scenarioId);
        _currentState = _simulationEngine.StartScenario(scenarioType, aircraft);
        _pilotActions.Clear();
        return _currentState;
    }

    public CockpitState SubmitPilotAction(string actionName)
    {
        if (_currentRun is null || _currentState is null || _currentAircraft is null || string.IsNullOrWhiteSpace(_currentScenarioType))
            throw new InvalidOperationException("No active simulation.");
        var nextStep = _pilotActions.Count + 1;
        var expectedStep = _expectedSteps.FirstOrDefault(step => step.StepOrder == nextStep);
        var correctAction = _simulationEngine.IsActionCorrect(_currentScenarioType, _currentAircraft, actionName, nextStep);
        _pilotActions.Add(new PilotAction
        {
            ScenarioRunId = _currentRun.Id,
            ActionName = actionName,
            StepOrder = nextStep,
            WasCorrect = correctAction,
            WasInCorrectOrder = correctAction,
            IsSafetyCritical = expectedStep?.IsSafetyCritical ?? false,
            PerformedAt = DateTime.UtcNow
        });
        _currentState = _simulationEngine.ApplyAction(_currentScenarioType, _currentState, actionName);
        return _currentState;
    }

    public SimulationReport CompleteSimulation()
    {
        if (_currentRun is null) throw new InvalidOperationException("No active simulation.");
        _currentRun.CompletedAt = DateTime.UtcNow;
        _currentRun.Status = "Completed";
        return Enrich(_scoringEngine.GenerateReport(_currentRun, _pilotActions, _expectedSteps));
    }

    public async Task<SimulationReport> CompleteAndSaveSimulationAsync()
    {
        var report = CompleteSimulation();
        return await dashboardService.SaveCompletedPracticeAsync(report);
    }

    private SimulationReport Enrich(SimulationReport report)
    {
        if (_currentRun is null) return report;
        report.PilotName = _pilotName;
        report.AircraftName = _currentRun.AircraftName;
        report.ScenarioName = _currentRun.ScenarioName;
        report.Difficulty = _difficulty;
        report.StartedAt = _currentRun.StartedAt;
        report.CompletedAt = _currentRun.CompletedAt ?? DateTime.UtcNow;
        report.ActionsTaken = _pilotActions.Count;
        report.ChecklistUsageScore = _expectedSteps.Count == 0 ? 0 : Math.Min(100, (int)Math.Round(_pilotActions.Count * 100d / _expectedSteps.Count));
        report.TimeManagementScore = Math.Clamp(100 - Math.Max(0, report.ReactionTimeSeconds - 5) * 3, 0, 100);
        report.CommunicationScore = _pilotActions.Any(x => x.ActionName.Contains("declare", StringComparison.OrdinalIgnoreCase)) ? 95 : 78;
        report.OverallScore = (int)Math.Round(report.ChecklistAccuracyScore * .40 + report.DecisionMakingScore * .25 + report.TimeManagementScore * .15 + report.CommunicationScore * .10 + report.ChecklistUsageScore * .10);
        report.AiFeedback = PerformanceDashboardService.GenerateAiStyleFeedback(report);
        return report;
    }

    public List<ScenarioProcedureStep> GetCurrentChecklist() => _expectedSteps;
    public List<PilotAction> GetPilotActions() => _pilotActions;
}