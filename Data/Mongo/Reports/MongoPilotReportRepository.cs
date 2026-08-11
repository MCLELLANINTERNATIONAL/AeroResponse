using AeroResponse.Models;
using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoPilotReportRepository
{
    private const string ReportsCollectionName =
        "simulationReports";

    private const string AchievementsCollectionName =
        "pilotAchievements";

    private readonly
        IMongoCollection<MongoSimulationReport> _reports;

    private readonly
        IMongoCollection<MongoPilotAchievement> _achievements;

    public MongoPilotReportRepository(
        MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _reports =
            context.GetCollection<MongoSimulationReport>(
                ReportsCollectionName);

        _achievements =
            context.GetCollection<MongoPilotAchievement>(
                AchievementsCollectionName);
    }

    public async Task EnsureIndexesAsync(
        CancellationToken cancellationToken = default)
    {
        var reportUserCreatedIndex =
            new CreateIndexModel<MongoSimulationReport>(
                Builders<MongoSimulationReport>
                    .IndexKeys
                    .Ascending(
                        report =>
                            report.UserId)
                    .Descending(
                        report =>
                            report.CreatedAt),
                new CreateIndexOptions
                {
                    Name =
                        "ix_simulationReports_userId_createdAt"
                });

        var reportCompletedAtIndex =
            new CreateIndexModel<MongoSimulationReport>(
                Builders<MongoSimulationReport>
                    .IndexKeys
                    .Descending(
                        report =>
                            report.CompletedAt),
                new CreateIndexOptions
                {
                    Name =
                        "ix_simulationReports_completedAt"
                });

        var reportUserCompletedAtIndex =
            new CreateIndexModel<MongoSimulationReport>(
                Builders<MongoSimulationReport>
                    .IndexKeys
                    .Ascending(
                        report =>
                            report.UserId)
                    .Descending(
                        report =>
                            report.CompletedAt),
                new CreateIndexOptions
                {
                    Name =
                        "ix_simulationReports_userId_completedAt"
                });

        var achievementUserEarnedIndex =
            new CreateIndexModel<MongoPilotAchievement>(
                Builders<MongoPilotAchievement>
                    .IndexKeys
                    .Ascending(
                        achievement =>
                            achievement.UserId)
                    .Descending(
                        achievement =>
                            achievement.EarnedAt),
                new CreateIndexOptions
                {
                    Name =
                        "ix_pilotAchievements_userId_earnedAt"
                });

        var achievementUserCodeIndex =
            new CreateIndexModel<MongoPilotAchievement>(
                Builders<MongoPilotAchievement>
                    .IndexKeys
                    .Ascending(
                        achievement =>
                            achievement.UserId)
                    .Ascending(
                        achievement =>
                            achievement.Code),
                new CreateIndexOptions
                {
                    Name =
                        "ux_pilotAchievements_userId_code",
                    Unique = true
                });

        await _reports.Indexes.CreateManyAsync(
            new[]
            {
                reportUserCreatedIndex,
                reportCompletedAtIndex,
                reportUserCompletedAtIndex
            },
            cancellationToken);

        await _achievements.Indexes.CreateManyAsync(
            new[]
            {
                achievementUserEarnedIndex,
                achievementUserCodeIndex
            },
            cancellationToken);
    }

    /// <summary>
    /// Returns platform-wide simulation reports directly
    /// from MongoDB, optionally constrained by completion date.
    ///
    /// This is used by the administration reporting dashboard
    /// so reporting does not fall back to the legacy SQL
    /// SimulationReports table.
    /// </summary>
    public async Task<IReadOnlyList<SimulationReport>>
        GetReportsAsync(
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default)
    {
        var filter =
            Builders<MongoSimulationReport>
                .Filter
                .Empty;

        if (fromUtc.HasValue)
        {
            filter &=
                Builders<MongoSimulationReport>
                    .Filter
                    .Gte(
                        report =>
                            report.CompletedAt,
                        fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            filter &=
                Builders<MongoSimulationReport>
                    .Filter
                    .Lte(
                        report =>
                            report.CompletedAt,
                        toUtc.Value);
        }

        var documents =
            await _reports
                .Find(filter)
                .SortByDescending(
                    report =>
                        report.CompletedAt)
                .ToListAsync(
                    cancellationToken);

        return documents
            .Select(
                document =>
                    document.ToModel())
            .ToArray();
    }

    /// <summary>
    /// Returns reports for a specific set of pilot user IDs
    /// directly from MongoDB, optionally constrained by
    /// completion date. Used by trainer/instructor reporting.
    /// </summary>
    public async Task<IReadOnlyList<SimulationReport>>
        GetReportsForUsersAsync(
            IEnumerable<string> userIds,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids =
            userIds
                .Where(userId =>
                    !string.IsNullOrWhiteSpace(userId))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<SimulationReport>();
        }

        var filter =
            Builders<MongoSimulationReport>
                .Filter
                .In(
                    report =>
                        report.UserId,
                    ids);

        if (fromUtc.HasValue)
        {
            filter &=
                Builders<MongoSimulationReport>
                    .Filter
                    .Gte(
                        report =>
                            report.CompletedAt,
                        fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            filter &=
                Builders<MongoSimulationReport>
                    .Filter
                    .Lte(
                        report =>
                            report.CompletedAt,
                        toUtc.Value);
        }

        var documents =
            await _reports
                .Find(filter)
                .SortBy(
                    report =>
                        report.CompletedAt)
                .ToListAsync(
                    cancellationToken);

        return documents
            .Select(
                document =>
                    document.ToModel())
            .ToArray();
    }

    public async Task<IReadOnlyList<SimulationReport>>
        GetReportsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<SimulationReport>();
        }

        var documents =
            await _reports
                .Find(
                    report =>
                        report.UserId == userId)
                .SortBy(
                    report =>
                        report.CreatedAt)
                .ToListAsync(
                    cancellationToken);

        return documents
            .Select(
                document =>
                    document.ToModel())
            .ToArray();
    }

    public async Task<IReadOnlyList<PilotAchievement>>
        GetAchievementsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<PilotAchievement>();
        }

        var documents =
            await _achievements
                .Find(
                    achievement =>
                        achievement.UserId == userId)
                .SortBy(
                    achievement =>
                        achievement.EarnedAt)
                .ToListAsync(
                    cancellationToken);

        return documents
            .Select(
                document =>
                    document.ToModel())
            .ToArray();
    }

    /// <summary>
    /// Saves or refreshes a simulation report without ever using the SQL
    /// integer identity as MongoDB's _id. The report fingerprint is based on
    /// the pilot plus the simulation start/completion timestamps, so resetting
    /// the relational database cannot cause an unrelated Mongo document to be
    /// overwritten when SQL IDs start again from 1.
    /// </summary>
    public async Task UpsertReportAsync(
        SimulationReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document =
            MongoSimulationReport.FromModel(
                report);

        await _reports.UpdateOneAsync(
            BuildReportIdentityFilter(
                report),
            BuildReportUpdate(
                document),
            new UpdateOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    /// <summary>
    /// Backfills historical SQL reports safely. Existing legacy Mongo records
    /// keep their original integer _id, while genuinely new records receive a
    /// Mongo ObjectId. SQL ID reuse can therefore never replace another report.
    /// </summary>
    public async Task UpsertReportsAsync(
        IEnumerable<SimulationReport> reports,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            reports);

        var writes =
            reports
                .Select(report =>
                {
                    var document =
                        MongoSimulationReport.FromModel(
                            report);

                    return
                        (WriteModel<MongoSimulationReport>)
                        new UpdateOneModel<MongoSimulationReport>(
                            BuildReportIdentityFilter(
                                report),
                            BuildReportUpdate(
                                document))
                        {
                            IsUpsert = true
                        };
                })
                .ToList();

        if (writes.Count == 0)
        {
            return;
        }

        await _reports.BulkWriteAsync(
            writes,
            new BulkWriteOptions
            {
                IsOrdered = false
            },
            cancellationToken);
    }

    /// <summary>
    /// Upserts achievements by their real logical identity: user + achievement
    /// code. SQL-generated integer IDs are retained only as optional legacy
    /// metadata and can safely repeat after a relational database reset.
    /// </summary>
    public async Task UpsertAchievementsAsync(
        IEnumerable<PilotAchievement> achievements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            achievements);

        var writes =
            achievements
                .Select(achievement =>
                {
                    var document =
                        MongoPilotAchievement.FromModel(
                            achievement);

                    var filter =
                        Builders<MongoPilotAchievement>
                            .Filter
                            .Eq(
                                stored =>
                                    stored.UserId,
                                document.UserId)
                        &
                        Builders<MongoPilotAchievement>
                            .Filter
                            .Eq(
                                stored =>
                                    stored.Code,
                                document.Code);

                    var update =
                        Builders<MongoPilotAchievement>
                            .Update
                            .SetOnInsert(
                                stored =>
                                    stored.Id,
                                document.Id)
                            .Set(
                                stored =>
                                    stored.LegacySqlAchievementId,
                                document.LegacySqlAchievementId)
                            .Set(
                                stored =>
                                    stored.UserId,
                                document.UserId)
                            .Set(
                                stored =>
                                    stored.Code,
                                document.Code)
                            .Set(
                                stored =>
                                    stored.Name,
                                document.Name)
                            .Set(
                                stored =>
                                    stored.Description,
                                document.Description)
                            .Set(
                                stored =>
                                    stored.Icon,
                                document.Icon)
                            .Set(
                                stored =>
                                    stored.EarnedAt,
                                document.EarnedAt);

                    return
                        (WriteModel<MongoPilotAchievement>)
                        new UpdateOneModel<MongoPilotAchievement>(
                            filter,
                            update)
                        {
                            IsUpsert = true
                        };
                })
                .ToList();

        if (writes.Count == 0)
        {
            return;
        }

        await _achievements.BulkWriteAsync(
            writes,
            new BulkWriteOptions
            {
                IsOrdered = false
            },
            cancellationToken);
    }

    private static FilterDefinition<MongoSimulationReport>
        BuildReportIdentityFilter(
            SimulationReport report)
    {
        // StartedAt and CompletedAt are generated by the simulation itself and
        // do not reset when SQLite/SQL is recreated. Combining them with UserId
        // gives a stable retry key without coupling MongoDB to SQL identities.
        return
            Builders<MongoSimulationReport>
                .Filter
                .Eq(
                    stored =>
                        stored.UserId,
                    report.UserId)
            &
            Builders<MongoSimulationReport>
                .Filter
                .Eq(
                    stored =>
                        stored.StartedAt,
                    report.StartedAt)
            &
            Builders<MongoSimulationReport>
                .Filter
                .Eq(
                    stored =>
                        stored.CompletedAt,
                    report.CompletedAt);
    }

    private static UpdateDefinition<MongoSimulationReport>
        BuildReportUpdate(
            MongoSimulationReport document)
    {
        return
            Builders<MongoSimulationReport>
                .Update
                // _id is written only when Mongo inserts a brand-new document.
                // Existing legacy integer IDs are intentionally preserved.
                .SetOnInsert(
                    stored =>
                        stored.Id,
                    document.Id)
                .Set(
                    stored =>
                        stored.LegacySqlReportId,
                    document.LegacySqlReportId)
                .Set(
                    stored =>
                        stored.ScenarioRunId,
                    document.ScenarioRunId)
                .Set(
                    stored =>
                        stored.UserId,
                    document.UserId)
                .Set(
                    stored =>
                        stored.PilotName,
                    document.PilotName)
                .Set(
                    stored =>
                        stored.AircraftName,
                    document.AircraftName)
                .Set(
                    stored =>
                        stored.ScenarioName,
                    document.ScenarioName)
                .Set(
                    stored =>
                        stored.Difficulty,
                    document.Difficulty)
                .Set(
                    stored =>
                        stored.StartedAt,
                    document.StartedAt)
                .Set(
                    stored =>
                        stored.CompletedAt,
                    document.CompletedAt)
                .Set(
                    stored =>
                        stored.TotalTimeSeconds,
                    document.TotalTimeSeconds)
                .Set(
                    stored =>
                        stored.ActionsTaken,
                    document.ActionsTaken)
                .Set(
                    stored =>
                        stored.ReactionTimeSeconds,
                    document.ReactionTimeSeconds)
                .Set(
                    stored =>
                        stored.ProcedureAccuracyScore,
                    document.ProcedureAccuracyScore)
                .Set(
                    stored =>
                        stored.DecisionMakingScore,
                    document.DecisionMakingScore)
                .Set(
                    stored =>
                        stored.ChecklistAccuracyScore,
                    document.ChecklistAccuracyScore)
                .Set(
                    stored =>
                        stored.ChecklistUsageScore,
                    document.ChecklistUsageScore)
                .Set(
                    stored =>
                        stored.TimeManagementScore,
                    document.TimeManagementScore)
                .Set(
                    stored =>
                        stored.CommunicationScore,
                    document.CommunicationScore)
                .Set(
                    stored =>
                        stored.OverallScore,
                    document.OverallScore)
                .Set(
                    stored =>
                        stored.SafetyCriticalErrors,
                    document.SafetyCriticalErrors)
                .Set(
                    stored =>
                        stored.Passed,
                    document.Passed)
                .Set(
                    stored =>
                        stored.Outcome,
                    document.Outcome)
                .Set(
                    stored =>
                        stored.Feedback,
                    document.Feedback)
                .Set(
                    stored =>
                        stored.AiFeedback,
                    document.AiFeedback)
                .Set(
                    stored =>
                        stored.CreatedAt,
                    document.CreatedAt);
    }
}