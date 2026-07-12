namespace AeroResponse.Models;

public class PilotAction
{
    public int Id { get; set; }
    public int ScenarioRunId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public bool WasCorrect { get; set; }
    public bool WasInCorrectOrder { get; set; }
    public bool IsSafetyCritical { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}