using AeroResponse.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoSimulationReport
{
    // MongoDB owns the document identity. Older records in this collection used
    // SQL integer IDs as _id values, so BsonValue is intentionally used here to
    // remain backwards-compatible with both legacy integers and new ObjectIds.
    [BsonId]
    public BsonValue Id { get; set; } = ObjectId.GenerateNewId();

    // Retained only as a reference to the legacy relational report. It is not
    // used as MongoDB's identity and therefore may safely repeat after a local
    // or Render SQL database reset.
    [BsonIgnoreIfNull]
    public int? LegacySqlReportId { get; set; }

    public int ScenarioRunId { get; set; }

    public string UserId { get; set; } =
        string.Empty;

    public string PilotName { get; set; } =
        string.Empty;

    public string AircraftName { get; set; } =
        string.Empty;

    public string ScenarioName { get; set; } =
        string.Empty;

    public string Difficulty { get; set; } =
        "Intermediate";

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

    public string Outcome { get; set; } =
        string.Empty;

    public string Feedback { get; set; } =
        string.Empty;

    public string AiFeedback { get; set; } =
        string.Empty;

    public DateTime CreatedAt { get; set; }

    public static MongoSimulationReport FromModel(
        SimulationReport report) =>
        new()
        {
            // Always generate a Mongo-native identity for a new document.
            // Repository update/upsert operations preserve an existing _id.
            Id = ObjectId.GenerateNewId(),

            LegacySqlReportId =
                report.Id > 0
                    ? report.Id
                    : null,

            ScenarioRunId =
                report.ScenarioRunId,

            UserId =
                report.UserId,

            PilotName =
                report.PilotName,

            AircraftName =
                report.AircraftName,

            ScenarioName =
                report.ScenarioName,

            Difficulty =
                report.Difficulty,

            StartedAt =
                report.StartedAt,

            CompletedAt =
                report.CompletedAt,

            TotalTimeSeconds =
                report.TotalTimeSeconds,

            ActionsTaken =
                report.ActionsTaken,

            ReactionTimeSeconds =
                report.ReactionTimeSeconds,

            ProcedureAccuracyScore =
                report.ProcedureAccuracyScore,

            DecisionMakingScore =
                report.DecisionMakingScore,

            ChecklistAccuracyScore =
                report.ChecklistAccuracyScore,

            ChecklistUsageScore =
                report.ChecklistUsageScore,

            TimeManagementScore =
                report.TimeManagementScore,

            CommunicationScore =
                report.CommunicationScore,

            OverallScore =
                report.OverallScore,

            SafetyCriticalErrors =
                report.SafetyCriticalErrors,

            Passed =
                report.Passed,

            Outcome =
                report.Outcome,

            Feedback =
                report.Feedback,

            AiFeedback =
                report.AiFeedback,

            CreatedAt =
                report.CreatedAt
        };

    public SimulationReport ToModel() =>
        new()
        {
            // Keep compatibility with legacy report consumers that still expose
            // an integer Id. New Mongo-native reports do not depend on this ID.
            Id =
                LegacySqlReportId
                ?? GetLegacyIntegerId(),

            ScenarioRunId =
                ScenarioRunId,

            UserId =
                UserId,

            PilotName =
                PilotName,

            AircraftName =
                AircraftName,

            ScenarioName =
                ScenarioName,

            Difficulty =
                Difficulty,

            StartedAt =
                StartedAt,

            CompletedAt =
                CompletedAt,

            TotalTimeSeconds =
                TotalTimeSeconds,

            ActionsTaken =
                ActionsTaken,

            ReactionTimeSeconds =
                ReactionTimeSeconds,

            ProcedureAccuracyScore =
                ProcedureAccuracyScore,

            DecisionMakingScore =
                DecisionMakingScore,

            ChecklistAccuracyScore =
                ChecklistAccuracyScore,

            ChecklistUsageScore =
                ChecklistUsageScore,

            TimeManagementScore =
                TimeManagementScore,

            CommunicationScore =
                CommunicationScore,

            OverallScore =
                OverallScore,

            SafetyCriticalErrors =
                SafetyCriticalErrors,

            Passed =
                Passed,

            Outcome =
                Outcome,

            Feedback =
                Feedback,

            AiFeedback =
                AiFeedback,

            CreatedAt =
                CreatedAt
        };

    private int GetLegacyIntegerId()
    {
        if (Id.IsInt32)
        {
            return Id.AsInt32;
        }

        if (Id.IsInt64 &&
            Id.AsInt64 >= int.MinValue &&
            Id.AsInt64 <= int.MaxValue)
        {
            return (int)Id.AsInt64;
        }

        return 0;
    }
}