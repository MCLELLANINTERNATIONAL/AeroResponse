namespace AeroResponse.Models;

public class PerformanceResult
{
    public int Id { get; set; }

    public int FlightLogId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int ReactionTimeSeconds { get; set; }

    public int ProcedureAccuracyScore { get; set; }

    public int DecisionMakingScore { get; set; }

    public int OverallScore { get; set; }

    public string Feedback { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}