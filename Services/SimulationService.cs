using AeroResponse.Data;
using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Services;

public sealed class SimulationService(
    SimulationEngine simulationEngine,
    PerformanceScoringEngine scoringEngine,
    PerformanceDashboardService dashboardService,
    AiInstructorService aiInstructor,
    ApplicationDbContext context)
{
    private ScenarioRun? _currentRun;

    private EmergencyScenario? _currentScenario;

    private CockpitState? _currentState;

    private CockpitLayoutDefinition? _currentAircraft;

    private string? _currentScenarioType;

    private string _pilotName = "Pilot";

    private string _difficulty = "Intermediate";

    private DateTime? _emergencyTriggeredAt;

    private List<ScenarioProcedureStep> _expectedSteps = [];

    private readonly List<PilotAction> _pilotActions = [];

    public bool HasActiveSimulation =>
        _currentRun?.Status == "In Progress";

    public DateTime? EmergencyTriggeredAt =>
        _emergencyTriggeredAt;

    public IReadOnlyList<PilotAction> PilotActions =>
        _pilotActions;

    public CockpitState StartSimulation(
        string userId,
        int aircraftId,
        EmergencyScenario scenario,
        CockpitLayoutDefinition aircraft,
        IReadOnlyList<ScenarioProcedureStep> expectedSteps,
        CockpitState initialState,
        string pilotName = "Pilot")
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(aircraft);
        ArgumentNullException.ThrowIfNull(expectedSteps);
        ArgumentNullException.ThrowIfNull(initialState);

        _currentAircraft = aircraft;
        _currentScenario = scenario;
        _currentScenarioType = scenario.EmergencyType;
        _pilotName = pilotName;
        _difficulty = scenario.Difficulty;
        _emergencyTriggeredAt = null;

        _currentRun = new ScenarioRun
        {
            UserId = userId,
            AircraftId = aircraftId,
            EmergencyScenarioId = scenario.Id,
            AircraftName = aircraft.Name,
            ScenarioName = scenario.Title,
            StartedAt = DateTime.UtcNow,
            Status = "In Progress"
        };

        _expectedSteps = expectedSteps
            .OrderBy(step => step.StepOrder)
            .ToList();

        _pilotActions.Clear();

        _currentState = initialState;

        return _currentState;
    }

    public CockpitState MarkEmergencyTriggered(
        CockpitState currentState,
        DateTime? triggeredAt = null)
    {
        ArgumentNullException.ThrowIfNull(
            currentState);

        if (_currentRun is null ||
            _currentScenario is null ||
            _currentAircraft is null)
        {
            throw new InvalidOperationException(
                "No active simulation.");
        }

        if (_emergencyTriggeredAt.HasValue)
        {
            return _currentState ?? currentState;
        }

        _emergencyTriggeredAt =
            triggeredAt ?? DateTime.UtcNow;

        _currentRun.StartedAt =
            _emergencyTriggeredAt.Value;

        _currentState =
            simulationEngine.StartScenario(
                _currentScenario.EmergencyType,
                _currentAircraft,
                currentState);

        return _currentState;
    }

    /// <summary>
    /// Records a normal pilot action and then lets the existing
    /// runtime scenario class update the cockpit state.
    /// </summary>
    public CockpitState SubmitPilotAction(
        string actionName,
        int selectedStepOrder)
    {
        EnsureActiveSimulation();
        EnsureEmergencyTriggered();

        RecordActionOnly(
            actionName,
            selectedStepOrder);

        _currentState =
            simulationEngine.ApplyAction(
                _currentScenarioType!,
                _currentState!,
                actionName);

        return _currentState;
    }

    /// <summary>
    /// Records an action when another service has already updated
    /// the cockpit state, such as a dynamic voice command.
    /// This prevents SimulationEngine.ApplyAction from running
    /// against the same action a second time.
    /// </summary>

    public CockpitState RecordPilotAction(
        string actionName,
        int selectedStepOrder,
        CockpitState externallyUpdatedState)
    {
        ArgumentNullException.ThrowIfNull(
            externallyUpdatedState);

        EnsureActiveSimulation();
        EnsureEmergencyTriggered();

        _currentState =
            externallyUpdatedState;

        RecordActionOnly(
            actionName,
            selectedStepOrder);

        return _currentState;
    }

    public bool IsTimedOut(
        DateTime? now = null)
    {
        if (_currentScenario is null ||
            !_emergencyTriggeredAt.HasValue ||
            _currentScenario.TimeLimitSeconds <= 0)
        {
            return false;
        }

        var currentTime =
            now ?? DateTime.UtcNow;

        return currentTime -
            _emergencyTriggeredAt.Value >=
            TimeSpan.FromSeconds(
                _currentScenario.TimeLimitSeconds);
    }

    public int GetRemainingSeconds(
        DateTime? now = null)
    {
        if (_currentScenario is null ||
            _currentScenario.TimeLimitSeconds <= 0)
        {
            return 0;
        }

        if (!_emergencyTriggeredAt.HasValue)
        {
            return _currentScenario.TimeLimitSeconds;
        }

        var currentTime =
            now ?? DateTime.UtcNow;

        var elapsedSeconds =
            (int)Math.Floor(
                (currentTime -
                _emergencyTriggeredAt.Value)
                .TotalSeconds);

        return Math.Max(
            0,
            _currentScenario.TimeLimitSeconds -
            elapsedSeconds);
    }

    public async Task<SimulationReport>
        CompleteAndSaveSimulationAsync(
            string? completionReason = null)
    {
        EnsureActiveSimulation();

        _currentRun!.CompletedAt =
            DateTime.UtcNow;

        _currentRun.Status =
            "Completed";

        _currentRun.Outcome =
            completionReason ?? string.Empty;

        context.ScenarioRuns.Add(
            _currentRun);

        await context.SaveChangesAsync();

        foreach (var action in _pilotActions)
        {
            action.ScenarioRunId =
                _currentRun.Id;
        }

        if (_pilotActions.Count > 0)
        {
            context.PilotActions.AddRange(
                _pilotActions);

            await context.SaveChangesAsync();
        }

        var report =
            scoringEngine.GenerateReport(
                _currentRun,
                _currentScenario!,
                _pilotActions,
                _expectedSteps);

        Enrich(report);

        report.AiFeedback =
            aiInstructor.GenerateFinalComment(
                report,
                _pilotActions);

        if (!string.IsNullOrWhiteSpace(
                completionReason))
        {
            report.Feedback =
                $"{report.Feedback} " +
                $"{completionReason}";

            report.Feedback =
                report.Feedback.Trim();
        }

        return await dashboardService
            .SaveCompletedPracticeAsync(
                report);
    }

    public CockpitState RecordStateStepCompletion(
        ScenarioProcedureStep step,
        CockpitState currentState)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(currentState);

        EnsureActiveSimulation();
        EnsureEmergencyTriggered();

        _currentState = currentState;

        RecordActionOnly(
            step.CorrectAction,
            step.StepOrder);

        return _currentState;
    }

    public IReadOnlyList<ScenarioProcedureStep>
        GetCurrentChecklist()
    {
        return _expectedSteps;
    }

    private void RecordActionOnly(
        string actionName,
        int selectedStepOrder)
    {
        var actionNumber =
            _pilotActions.Count + 1;

        var matchingStep =
            _expectedSteps.FirstOrDefault(
                step =>
                    string.Equals(
                        step.CorrectAction,
                        actionName,
                        StringComparison
                            .OrdinalIgnoreCase));

        var nextExpectedStep =
            _expectedSteps
                .Where(step =>
                    !_pilotActions.Any(
                        action =>
                            action.WasCorrect &&
                            action.WasInCorrectOrder &&
                            action.ExpectedStepOrder ==
                                step.StepOrder))
                .OrderBy(step =>
                    step.StepOrder)
                .FirstOrDefault();

        var wasCorrect =
            matchingStep is not null;

        var wasInCorrectOrder =
            wasCorrect &&
            nextExpectedStep?.StepOrder ==
            matchingStep!.StepOrder &&
            selectedStepOrder ==
            matchingStep.StepOrder;

        var responseSeconds =
            Math.Max(
                0,
                (int)Math.Round(
                    (DateTime.UtcNow -
                    _emergencyTriggeredAt!.Value)
                    .TotalSeconds));

        _pilotActions.Add(
            new PilotAction
            {
                ActionName =
                    actionName,

                StepOrder =
                    actionNumber,

                ExpectedStepOrder =
                    matchingStep?.StepOrder,

                WasCorrect =
                    wasCorrect,

                WasInCorrectOrder =
                    wasInCorrectOrder,

                WasWithinTimeLimit =
                    matchingStep is not null &&
                    responseSeconds <=
                    matchingStep
                        .MaxResponseSeconds,

                IsSafetyCritical =
                    matchingStep?
                        .IsSafetyCritical ?? false,

                ResponseTimeSeconds =
                    responseSeconds,

                PerformedAt =
                    DateTime.UtcNow
            });
    }

    private void Enrich(
        SimulationReport report)
    {
        report.UserId =
            _currentRun!.UserId;

        report.PilotName =
            _pilotName;

        report.AircraftName =
            _currentRun.AircraftName;

        report.ScenarioName =
            _currentRun.ScenarioName;

        report.Difficulty =
            _difficulty;

        report.StartedAt =
            _currentRun.StartedAt;

        report.CompletedAt =
            _currentRun.CompletedAt
            ?? DateTime.UtcNow;
    }

    private void EnsureEmergencyTriggered()
    {
        if (!_emergencyTriggeredAt.HasValue)
        {
            throw new InvalidOperationException(
                "The emergency has not been triggered yet.");
        }
    }

    private void EnsureActiveSimulation()
    {
        if (_currentRun is null ||
            _currentScenario is null ||
            _currentState is null ||
            _currentAircraft is null ||
            string.IsNullOrWhiteSpace(
                _currentScenarioType) ||
            _currentRun.Status != "In Progress")
        {
            throw new InvalidOperationException(
                "No active simulation.");
        }
    }
}