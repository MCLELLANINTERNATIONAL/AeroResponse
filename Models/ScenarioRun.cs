namespace AeroResponse.Models;

public class ScenarioRun
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int AircraftId { get; set; }
    public int EmergencyScenarioId { get; set; }
    public string AircraftName { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "In Progress";
    public string Outcome { get; set; } = string.Empty;
}