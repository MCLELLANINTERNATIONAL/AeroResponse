namespace AeroResponse.Models;

public class EmergencyScenario
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EmergencyType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Beginner";

    // Human-readable explanation of the event that starts the emergency.
    public string TriggerCondition { get; set; } = string.Empty;

    // Configurable trigger fields used by the interactive simulator.
    public string TriggerType { get; set; } = "Immediate";

    public int? TriggerDelaySeconds { get; set; }

    public double? TriggerAltitudeFeet { get; set; }

    public double? TriggerAirspeedKnots { get; set; }

    public string? TriggerFlightPhase { get; set; }

    public bool RequiresManualActivation { get; set; }

    // Maximum time allowed after the emergency is triggered.
    public int TimeLimitSeconds { get; set; } = 120;

    // Human-readable rules displayed to instructors and used in reports.
    public string SuccessCondition { get; set; } =
        "Complete all safety-critical actions and achieve an overall score of at least 70%.";

    public string FailureCondition { get; set; } =
        "The time limit expires, a safety-critical action is missed, or the overall score is below 70%.";

    public string ScoringRules { get; set; } =
        "Procedure 40%; decision making 25%; time management 15%; communication 10%; checklist usage 10%.";

    // Ordered procedure text. Detailed executable steps can also be stored
    // in ScenarioProcedureSteps for aircraft-specific behaviour.
    public string ExpectedProcedure { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ScenarioProcedureStep> ProcedureSteps { get; set; } = [];
}