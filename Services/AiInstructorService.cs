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
            return new() { Severity = "Warning", Message = result.SpokenFeedback };

        var next = expected.Where(step => !completed.Any(action => action.WasCorrect &&
                action.ExpectedStepOrder == step.StepOrder))
            .OrderBy(step => step.StepOrder).FirstOrDefault();

        var matched = expected.FirstOrDefault(step => step.CorrectAction.Equals(
            result.ActionName, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            return new()
            {
                Severity = "Caution",
                Message = result.SpokenFeedback +
                          " This action is not part of the current gold-standard checklist.",
                RecommendedAction = next?.Instruction
            };

        if (next is not null && next.StepOrder != matched.StepOrder)
            return new()
            {
                Severity = matched.IsSafetyCritical ? "Critical" : "Caution",
                Message = $"Valid action, but out of sequence. Next expected step: {next.Instruction}",
                RecommendedAction = next.Instruction
            };

        return new()
        {
            Severity = "Positive",
            Message = $"Correct. {matched.Instruction}" +
                      (remainingSeconds <= 15 ? " Time is becoming critical." : string.Empty),
            RecommendedAction = expected.FirstOrDefault(step =>
                step.StepOrder == matched.StepOrder + 1)?.Instruction
        };
    }

    public string GenerateFinalComment(SimulationReport report,
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
            comments.Add($"{report.SafetyCriticalErrors} safety-critical error or omission was recorded.");

        var outOfOrder = actions.Count(action => action.WasCorrect && !action.WasInCorrectOrder);
        if (outOfOrder > 0)
            comments.Add($"{outOfOrder} correct action or actions were completed out of sequence.");

        comments.Add("Recommended next focus: practise the first three memory actions until they are accurate and immediate.");
        return string.Join(" ", comments);
    }
}