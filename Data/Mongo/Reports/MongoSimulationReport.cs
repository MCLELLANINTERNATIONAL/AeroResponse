using AeroResponse.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoSimulationReport
{
    [BsonId]
    public int Id { get; set; }

    public int ScenarioRunId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PilotName { get; set; } = string.Empty;
    public string AircraftName { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Intermediate";
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
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
    public string Outcome { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public string AiFeedback { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static MongoSimulationReport FromModel(SimulationReport report) => new()
    {
        Id = report.Id,
        ScenarioRunId = report.ScenarioRunId,
        UserId = report.UserId,
        PilotName = report.PilotName,
        AircraftName = report.AircraftName,
        ScenarioName = report.ScenarioName,
        Difficulty = report.Difficulty,
        StartedAt = report.StartedAt,
        CompletedAt = report.CompletedAt,
        TotalTimeSeconds = report.TotalTimeSeconds,
        ActionsTaken = report.ActionsTaken,
        ReactionTimeSeconds = report.ReactionTimeSeconds,
        ProcedureAccuracyScore = report.ProcedureAccuracyScore,
        DecisionMakingScore = report.DecisionMakingScore,
        ChecklistAccuracyScore = report.ChecklistAccuracyScore,
        ChecklistUsageScore = report.ChecklistUsageScore,
        TimeManagementScore = report.TimeManagementScore,
        CommunicationScore = report.CommunicationScore,
        OverallScore = report.OverallScore,
        SafetyCriticalErrors = report.SafetyCriticalErrors,
        Passed = report.Passed,
        Outcome = report.Outcome,
        Feedback = report.Feedback,
        AiFeedback = report.AiFeedback,
        CreatedAt = report.CreatedAt
    };

    public SimulationReport ToModel() => new()
    {
        Id = Id,
        ScenarioRunId = ScenarioRunId,
        UserId = UserId,
        PilotName = PilotName,
        AircraftName = AircraftName,
        ScenarioName = ScenarioName,
        Difficulty = Difficulty,
        StartedAt = StartedAt,
        CompletedAt = CompletedAt,
        TotalTimeSeconds = TotalTimeSeconds,
        ActionsTaken = ActionsTaken,
        ReactionTimeSeconds = ReactionTimeSeconds,
        ProcedureAccuracyScore = ProcedureAccuracyScore,
        DecisionMakingScore = DecisionMakingScore,
        ChecklistAccuracyScore = ChecklistAccuracyScore,
        ChecklistUsageScore = ChecklistUsageScore,
        TimeManagementScore = TimeManagementScore,
        CommunicationScore = CommunicationScore,
        OverallScore = OverallScore,
        SafetyCriticalErrors = SafetyCriticalErrors,
        Passed = Passed,
        Outcome = Outcome,
        Feedback = Feedback,
        AiFeedback = AiFeedback,
        CreatedAt = CreatedAt
    };
}
