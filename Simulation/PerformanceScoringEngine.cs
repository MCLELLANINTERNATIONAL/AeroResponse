using AeroResponse.Models;

namespace AeroResponse.Simulation;

public class PerformanceScoringEngine
{
    public SimulationReport GenerateReport(
        ScenarioRun run,
        List<PilotAction> actions,
        List<ScenarioProcedureStep> expectedSteps)
    {
        var correctActions = actions.Count(a => a.WasCorrect);
        var totalSteps = expectedSteps.Count;
        var safetyCriticalErrors = actions.Count(a => a.IsSafetyCritical && !a.WasCorrect);

        var checklistAccuracy = totalSteps == 0
            ? 0
            : (int)Math.Round((double)correctActions / totalSteps * 100);

        var decisionScore = Math.Max(0, 100 - (safetyCriticalErrors * 25));

        var reactionTime = actions.Count == 0
            ? 0
            : (int)(actions.Min(a => a.PerformedAt) - run.StartedAt).TotalSeconds;

        var overall = (checklistAccuracy + decisionScore) / 2;

        return new SimulationReport
        {
            ScenarioRunId = run.Id,
            UserId = run.UserId,
            ReactionTimeSeconds = Math.Max(0, reactionTime),
            ChecklistAccuracyScore = checklistAccuracy,
            DecisionMakingScore = decisionScore,
            OverallScore = overall,
            SafetyCriticalErrors = safetyCriticalErrors,
            Outcome = overall >= 80 ? "Aircraft stabilized and passengers safeguarded." : "Emergency response requires improvement.",
            Feedback = GenerateFeedback(overall, safetyCriticalErrors)
        };
    }

    private static string GenerateFeedback(int overallScore, int safetyCriticalErrors)
    {
        if (overallScore >= 90)
        {
            return "Excellent response. Checklist actions were completed accurately and safely.";
        }

        if (overallScore >= 75)
        {
            return "Good response. Review timing and checklist order for further improvement.";
        }

        if (safetyCriticalErrors > 0)
        {
            return "Safety-critical errors were recorded. Review the emergency procedure before attempting this scenario again.";
        }

        return "Additional practice recommended. Focus on checklist completion and decision-making sequence.";
    }
}