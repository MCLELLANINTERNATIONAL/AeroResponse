using AeroResponse.Models;
using AeroResponse.Simulation;
using AeroResponse.Simulation.Controls;

namespace AeroResponse.Services;

public sealed class AiInstructorService
{
    public AiInstructorFeedback EvaluateAction(
        CockpitCommandResult result,
        IReadOnlyList<ScenarioProcedureStep> expected,
        IReadOnlyList<PilotAction> completed,
        int remainingSeconds)
    {
        if (!result.Succeeded)
        {
            return new AiInstructorFeedback
            {
                Severity = "Warning",

                Message =
                    "Incorrect. This action is not part of the current " +
                    "gold-standard checklist. Please try another option.",

                RecommendedAction = null
            };
        }

        var matched =
            expected.FirstOrDefault(step =>
                string.Equals(
                    step.CorrectAction,
                    result.ActionName,
                    StringComparison.OrdinalIgnoreCase));

        var nextIncomplete =
            expected
                .Where(step =>
                    !completed.Any(action =>
                        action.WasCorrect &&
                        action.ExpectedStepOrder ==
                        step.StepOrder))
                .OrderBy(step => step.StepOrder)
                .FirstOrDefault();

        if (matched is null)
        {
            return new AiInstructorFeedback
            {
                Severity = "Caution",

                Message =
                    "Incorrect. This action is not part of the current " +
                    "gold-standard checklist. Please try another option.",

                RecommendedAction = null
            };
        }

        var recordedAction =
            completed.LastOrDefault(action =>
                action.ExpectedStepOrder ==
                matched.StepOrder &&
                string.Equals(
                    action.ActionName,
                    result.ActionName,
                    StringComparison.OrdinalIgnoreCase));

        if (recordedAction is not null &&
            !recordedAction.WasInCorrectOrder)
        {
            return new AiInstructorFeedback
            {
                Severity =
                    matched.IsSafetyCritical
                        ? "Critical"
                        : "Caution",

                Message =
                    "Valid action, but out of sequence. " +
                    "Follow the checklist in the displayed order.",

                RecommendedAction =
                    nextIncomplete?.Instruction
            };
        }

        return new AiInstructorFeedback
        {
            Severity = "Positive",

            Message =
                $"Correct. {matched.Instruction}" +
                (remainingSeconds <= 15
                    ? " Time is becoming critical."
                    : string.Empty),

            RecommendedAction =
                nextIncomplete?.Instruction
        };
    }

    public string GenerateFinalComment(
        SimulationReport report,
        IReadOnlyList<PilotAction> actions)
    {
        var comments = new List<string>
        {
            report.Passed
                ? "The emergency response achieved the required standard."
                : "The emergency response did not yet achieve the required standard.",

            report.ProcedureAccuracyScore >= 85
                ? "Procedure accuracy was strong."
                : "Review the checklist sequence and complete every required action.",

            report.ReactionTimeSeconds <= 10
                ? "The initial response was prompt."
                : "The initial response should be faster."
        };

        if (report.SafetyCriticalErrors > 0)
        {
            comments.Add(
                $"{report.SafetyCriticalErrors} safety-critical " +
                "error or omission was recorded.");
        }

        var outOfOrder =
            actions.Count(action =>
                action.WasCorrect &&
                !action.WasInCorrectOrder);

        if (outOfOrder > 0)
        {
            comments.Add(
                $"{outOfOrder} correct action or actions " +
                "were completed out of sequence.");
        }

        comments.Add(
            "Recommended next focus: practise the first three " +
            "memory actions until they are accurate and immediate.");

        return string.Join(" ", comments);
    }
}