using AeroResponse.Models;
using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Reports;

public sealed class MongoPilotReportRepository
{
    private const string ReportsCollectionName = "simulationReports";
    private const string AchievementsCollectionName = "pilotAchievements";

    private readonly IMongoCollection<MongoSimulationReport> _reports;
    private readonly IMongoCollection<MongoPilotAchievement> _achievements;

    public MongoPilotReportRepository(MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _reports = context.GetCollection<MongoSimulationReport>(ReportsCollectionName);
        _achievements = context.GetCollection<MongoPilotAchievement>(AchievementsCollectionName);
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var reportUserCreatedIndex = new CreateIndexModel<MongoSimulationReport>(
            Builders<MongoSimulationReport>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "ix_simulationReports_userId_createdAt" });

        var achievementUserEarnedIndex = new CreateIndexModel<MongoPilotAchievement>(
            Builders<MongoPilotAchievement>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.EarnedAt),
            new CreateIndexOptions { Name = "ix_pilotAchievements_userId_earnedAt" });

        var achievementUserCodeIndex = new CreateIndexModel<MongoPilotAchievement>(
            Builders<MongoPilotAchievement>.IndexKeys
                .Ascending(x => x.UserId)
                .Ascending(x => x.Code),
            new CreateIndexOptions
            {
                Name = "ux_pilotAchievements_userId_code",
                Unique = true
            });

        await _reports.Indexes.CreateOneAsync(
            reportUserCreatedIndex,
            cancellationToken: cancellationToken);

        await _achievements.Indexes.CreateManyAsync(
            new[] { achievementUserEarnedIndex, achievementUserCodeIndex },
            cancellationToken);
    }

    public async Task<IReadOnlyList<SimulationReport>> GetReportsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<SimulationReport>();
        }

        var documents = await _reports
            .Find(x => x.UserId == userId)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(x => x.ToModel()).ToArray();
    }

    public async Task<IReadOnlyList<PilotAchievement>> GetAchievementsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<PilotAchievement>();
        }

        var documents = await _achievements
            .Find(x => x.UserId == userId)
            .SortBy(x => x.EarnedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(x => x.ToModel()).ToArray();
    }

    public async Task UpsertReportAsync(
        SimulationReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document = MongoSimulationReport.FromModel(report);
        await _reports.ReplaceOneAsync(
            x => x.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task UpsertReportsAsync(
        IEnumerable<SimulationReport> reports,
        CancellationToken cancellationToken = default)
    {
        var writes = reports
            .Select(MongoSimulationReport.FromModel)
            .Select(document => (WriteModel<MongoSimulationReport>)new ReplaceOneModel<MongoSimulationReport>(
                Builders<MongoSimulationReport>.Filter.Eq(x => x.Id, document.Id),
                document)
            {
                IsUpsert = true
            })
            .ToList();

        if (writes.Count > 0)
        {
            await _reports.BulkWriteAsync(writes, cancellationToken: cancellationToken);
        }
    }

    public async Task UpsertAchievementsAsync(
        IEnumerable<PilotAchievement> achievements,
        CancellationToken cancellationToken = default)
    {
        var writes = achievements
            .Select(MongoPilotAchievement.FromModel)
            .Select(document => (WriteModel<MongoPilotAchievement>)new ReplaceOneModel<MongoPilotAchievement>(
                Builders<MongoPilotAchievement>.Filter.Eq(x => x.Id, document.Id),
                document)
            {
                IsUpsert = true
            })
            .ToList();

        if (writes.Count > 0)
        {
            await _achievements.BulkWriteAsync(writes, cancellationToken: cancellationToken);
        }
    }
}
