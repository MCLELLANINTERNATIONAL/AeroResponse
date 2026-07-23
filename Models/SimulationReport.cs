using System.ComponentModel.DataAnnotations;

namespace AeroResponse.Models;

public class SimulationReport
{
    public int Id { get; set; }

    public int ScenarioRunId { get; set; }

    [MaxLength(20)] public string UserId { get; set; } = string.Empty;

    [MaxLength(50)] public string PilotName { get; set; } = string.Empty;

    [MaxLength(50)] public string AircraftName { get; set; } = string.Empty;

    [MaxLength(100)] public string ScenarioName { get; set; } = string.Empty;

    [MaxLength(20)] public string Difficulty { get; set; } = "Intermediate";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public int TotalTimeSeconds { get; set; }

    public int ActionsTaken { get; set; }

    public int ReactionTimeSeconds { get; set; }

    public int ProcedureAccuracyScore { get; set; }

    public int DecisionMakingScore { get; set; }

    public int ChecklistAccuracyScore { get; set; }

    public int ChecklistUsageScore { get; set; }

    public int TimeManagementScore { get; set; }

    public int CommunicationScore { get; set; }

    public int OverallScore { get; set; }

    public int SafetyCriticalErrors { get; set; }

    public bool Passed { get; set; }

    [MaxLength(500)] public string Outcome { get; set; } = string.Empty;

    [MaxLength(2000)] public string Feedback { get; set; } = string.Empty;

    [MaxLength(2000)] public string AiFeedback { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}