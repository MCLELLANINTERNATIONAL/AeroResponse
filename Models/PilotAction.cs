namespace AeroResponse.Models;

public class PilotAction
{
    public int Id { get; set; }

    public int ScenarioRunId { get; set; }

    public string ActionName { get; set; } = string.Empty;

    // The order in which the pilot actually performed the action.
    public int StepOrder { get; set; }

    // The expected procedure step matched by this action, when one exists.
    public int? ExpectedStepOrder { get; set; }

    public bool WasCorrect { get; set; }

    public bool WasInCorrectOrder { get; set; }

    public bool WasWithinTimeLimit { get; set; }

    public bool IsSafetyCritical { get; set; }

    // Seconds elapsed from emergency activation to this action.
    public int ResponseTimeSeconds { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}

