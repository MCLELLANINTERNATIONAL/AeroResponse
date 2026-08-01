using AeroResponse.Models;

namespace AeroResponse.Simulation;

public sealed class PerformanceScoringEngine
{
    private const int PassingScore = 70;

    public SimulationReport GenerateReport(
        ScenarioRun run,
        EmergencyScenario scenario,
        IReadOnlyList<PilotAction> actions,
        IReadOnlyList<ScenarioProcedureStep> expectedSteps)
    {
        var completedAt = run.CompletedAt ?? DateTime.UtcNow;
        var totalTimeSeconds = Math.Max(
            0,
            (int)Math.Round((completedAt - run.StartedAt).TotalSeconds));

        var orderedSteps = expectedSteps
            .OrderBy(step => step.StepOrder)
            .ToList();

        var matchedCorrectSteps = actions
            .Where(action => action.WasCorrect && action.ExpectedStepOrder.HasValue)
            .GroupBy(action => action.ExpectedStepOrder!.Value)
            .Select(group => group.First())
            .ToList();

        var totalWeight = orderedSteps.Sum(step => Math.Max(1, step.ScoreWeight));
        var earnedProcedureWeight = orderedSteps.Sum(step =>
        {
            var action = matchedCorrectSteps.FirstOrDefault(item =>
                item.ExpectedStepOrder == step.StepOrder);

            if (action is null)
            {
                return 0d;
            }

            var weight = Math.Max(1, step.ScoreWeight);
            var orderMultiplier = action.WasInCorrectOrder ? 1d : 0.65d;
            var timingMultiplier = action.WasWithinTimeLimit ? 1d : 0.70d;

            return weight * orderMultiplier * timingMultiplier;
        });

        var procedureAccuracy = totalWeight == 0
            ? 0
            : (int)Math.Round(earnedProcedureWeight / totalWeight * 100d);

        var checklistUsage = orderedSteps.Count == 0
            ? 0
            : (int)Math.Round(
                matchedCorrectSteps.Count * 100d / orderedSteps.Count);

        var incorrectActions = actions.Count(action => !action.WasCorrect);
        var outOfOrderActions = actions.Count(action =>
            action.WasCorrect && !action.WasInCorrectOrder);
        var safetyCriticalErrors = orderedSteps.Count(step =>
            step.IsSafetyCritical &&
            matchedCorrectSteps.All(action =>
                action.ExpectedStepOrder != step.StepOrder));

        var decisionScore = Math.Clamp(
            100 -
            (incorrectActions * 10) -
            (outOfOrderActions * 8) -
            (safetyCriticalErrors * 25),
            0,
            100);

        var reactionTime = actions.Count == 0
            ? scenario.TimeLimitSeconds
            : Math.Max(0, actions.Min(action => action.ResponseTimeSeconds));

        var timeManagementScore = actions.Count == 0
            ? 0
            : CalculateTimeManagementScore(
                scenario.TimeLimitSeconds,
                reactionTime,
                actions,
                orderedSteps);

        var communicationSteps = orderedSteps
            .Where(step =>
                step.PerformanceCategory.Equals(
                    "Communication",
                    StringComparison.OrdinalIgnoreCase) ||
                step.CorrectAction.Contains(
                    "Declare",
                    StringComparison.OrdinalIgnoreCase) ||
                step.CorrectAction.Contains(
                    "Communicat",
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        var communicationScore = communicationSteps.Count == 0
            ? 100
            : (int)Math.Round(
                communicationSteps.Count(step =>
                    matchedCorrectSteps.Any(action =>
                        action.ExpectedStepOrder == step.StepOrder)) *
                100d / communicationSteps.Count);

        var overallScore = actions.Count == 0
            ? 0
            : (int)Math.Round(
                procedureAccuracy * 0.40 +
                decisionScore * 0.25 +
                timeManagementScore * 0.15 +
                communicationScore * 0.10 +
                checklistUsage * 0.10);

        var timedOut = totalTimeSeconds >= scenario.TimeLimitSeconds;
        var allCriticalActionsCompleted = safetyCriticalErrors == 0;
        var passed = overallScore >= PassingScore &&
                     allCriticalActionsCompleted &&
                     !timedOut;

        return new SimulationReport
        {
            ScenarioRunId = run.Id,
            UserId = run.UserId,
            StartedAt = run.StartedAt,
            CompletedAt = completedAt,
            TotalTimeSeconds = totalTimeSeconds,
            ActionsTaken = actions.Count,
            ReactionTimeSeconds = reactionTime,
            ProcedureAccuracyScore = procedureAccuracy,
            ChecklistAccuracyScore = procedureAccuracy,
            ChecklistUsageScore = checklistUsage,
            DecisionMakingScore = decisionScore,
            TimeManagementScore = timeManagementScore,
            CommunicationScore = communicationScore,
            OverallScore = overallScore,
            SafetyCriticalErrors = safetyCriticalErrors,
            Passed = passed,
            Outcome = BuildOutcome(passed, timedOut, actions.Count, safetyCriticalErrors),
            Feedback = BuildFeedback(
                actions.Count,
                reactionTime,
                procedureAccuracy,
                outOfOrderActions,
                safetyCriticalErrors,
                timedOut)
        };
    }

    private static int CalculateTimeManagementScore(
        int scenarioTimeLimitSeconds,
        int reactionTimeSeconds,
        IReadOnlyList<PilotAction> actions,
        IReadOnlyList<ScenarioProcedureStep> steps)
    {
        var reactionComponent = Math.Clamp(
            100 - Math.Max(0, reactionTimeSeconds - 5) * 4,
            0,
            100);

        var matchedActions = actions
            .Where(action => action.WasCorrect && action.ExpectedStepOrder.HasValue)
            .ToList();

        var onTimeCount = steps.Count(step =>
            matchedActions.Any(action =>
                action.ExpectedStepOrder == step.StepOrder &&
                action.WasWithinTimeLimit));

        var stepTimingComponent = steps.Count == 0
            ? 0
            : (int)Math.Round(onTimeCount * 100d / steps.Count);

        var finalActionSeconds = actions.Max(action => action.ResponseTimeSeconds);
        var completionComponent = finalActionSeconds <= scenarioTimeLimitSeconds
            ? 100
            : 0;

        return (int)Math.Round(
            reactionComponent * 0.40 +
            stepTimingComponent * 0.40 +
            completionComponent * 0.20);
    }

    private static string BuildOutcome(
        bool passed,
        bool timedOut,
        int actionCount,
        int safetyCriticalErrors)
    {
        if (actionCount == 0)
        {
            return "Scenario failed. No emergency actions were performed before the assessment ended.";
        }

        if (timedOut)
        {
            return "Scenario failed because the configured emergency-response time limit expired.";
        }

        if (safetyCriticalErrors > 0)
        {
            return "Scenario failed because one or more safety-critical actions were missed.";
        }

        return passed
            ? "Scenario passed. The required emergency actions were completed safely within the assessment limit."
            : "Scenario completed, but the response did not reach the required passing score.";
    }

    private static string BuildFeedback(
        int actionCount,
        int reactionTime,
        int procedureAccuracy,
        int outOfOrderActions,
        int safetyCriticalErrors,
        bool timedOut)
    {
        if (actionCount == 0)
        {
            return "No pilot response was recorded. Review the emergency checklist and begin with the first safety-critical action immediately after the warning appears.";
        }

        var messages = new List<string>();

        messages.Add(reactionTime <= 10
            ? "The initial response was prompt."
            : "Respond to the primary warning more quickly.");

        messages.Add(procedureAccuracy >= 85
            ? "The procedure was completed with strong accuracy."
            : "Review the expected procedure and complete every required action.");

        if (outOfOrderActions > 0)
        {
            messages.Add("Some correct actions were performed out of sequence.");
        }

        if (safetyCriticalErrors > 0)
        {
            messages.Add("One or more safety-critical actions were omitted.");
        }

        if (timedOut)
        {
            messages.Add("The assessment time limit expired before a successful response was completed.");
        }

        return string.Join(" ", messages);
    }
}
