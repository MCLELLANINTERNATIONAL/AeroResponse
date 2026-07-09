namespace AeroResponse.Models;

public class SimulationReport
{
    public int Id { get; set; }
    public int ScenarioRunId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ReactionTimeSeconds { get; set; }
    public int ChecklistAccuracyScore { get; set; }
    public int DecisionMakingScore { get; set; }
    public int OverallScore { get; set; }
    public int SafetyCriticalErrors { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}