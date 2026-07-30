using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Memberships;

public sealed class MongoMemberTimelineRepository
{
    private const string CollectionName =
        "memberTimeline";

    private readonly
        IMongoCollection<MongoMemberTimeline> _collection;

    public MongoMemberTimelineRepository(
        MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _collection =
            context.GetCollection<MongoMemberTimeline>(
                CollectionName);
    }

    public async Task<MongoMemberTimeline?>
        FindByIdentityUserIdAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return null;
        }

        return await _collection
            .Find(
                timeline =>
                    timeline.IdentityUserId ==
                    identityUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        MongoMemberTimeline timeline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        var filter =
            Builders<MongoMemberTimeline>
                .Filter
                .Eq(
                    membership =>
                        membership.IdentityUserId,
                    timeline.IdentityUserId);

        await _collection.ReplaceOneAsync(
            filter,
            timeline,
            new ReplaceOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }
}