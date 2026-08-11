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
                reportCompletedAtIndex
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

    public async Task UpsertReportAsync(
        SimulationReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document =
            MongoSimulationReport.FromModel(
                report);

        await _reports.ReplaceOneAsync(
            storedReport =>
                storedReport.Id == document.Id,
            document,
            new ReplaceOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    public async Task UpsertReportsAsync(
        IEnumerable<SimulationReport> reports,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reports);

        var writes =
            reports
                .Select(
                    MongoSimulationReport.FromModel)
                .Select(
                    document =>
                        (WriteModel<MongoSimulationReport>)
                        new ReplaceOneModel<MongoSimulationReport>(
                            Builders<MongoSimulationReport>
                                .Filter
                                .Eq(
                                    storedReport =>
                                        storedReport.Id,
                                    document.Id),
                            document)
                        {
                            IsUpsert = true
                        })
                .ToList();

        if (writes.Count == 0)
        {
            return;
        }

        await _reports.BulkWriteAsync(
            writes,
            cancellationToken:
                cancellationToken);
    }

    public async Task UpsertAchievementsAsync(
        IEnumerable<PilotAchievement> achievements,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(achievements);

        var writes =
            achievements
                .Select(
                    MongoPilotAchievement.FromModel)
                .Select(
                    document =>
                        (WriteModel<MongoPilotAchievement>)
                        new ReplaceOneModel<MongoPilotAchievement>(
                            Builders<MongoPilotAchievement>
                                .Filter
                                .Eq(
                                    storedAchievement =>
                                        storedAchievement.Id,
                                    document.Id),
                            document)
                        {
                            IsUpsert = true
                        })
                .ToList();

        if (writes.Count == 0)
        {
            return;
        }

        await _achievements.BulkWriteAsync(
            writes,
            cancellationToken:
                cancellationToken);
    }
}