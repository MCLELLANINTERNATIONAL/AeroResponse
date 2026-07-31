namespace AeroResponse.Models;

public class EmergencyScenario
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EmergencyType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Beginner";

    // Optional human-readable explanation.
    public string TriggerCondition { get; set; } = string.Empty;

    // New configurable trigger fields.
    public string TriggerType { get; set; } = "Immediate";

    public int? TriggerDelaySeconds { get; set; }

    public double? TriggerAltitudeFeet { get; set; }

    public double? TriggerAirspeedKnots { get; set; }

    public string? TriggerFlightPhase { get; set; }

    public bool RequiresManualActivation { get; set; }

    public string ExpectedProcedure { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}