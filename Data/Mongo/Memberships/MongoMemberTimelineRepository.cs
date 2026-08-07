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
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
        MongoMemberTimeline timeline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        if (string.IsNullOrWhiteSpace(
            timeline.IdentityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(timeline));
        }

        var filter =
            Builders<MongoMemberTimeline>
                .Filter
                .Eq(
                    membership =>
                        membership.IdentityUserId,
                    timeline.IdentityUserId);

        var update =
            Builders<MongoMemberTimeline>
                .Update
                .SetOnInsert(
                    membership =>
                        membership.IdentityUserId,
                    timeline.IdentityUserId)
                .Set(
                    membership =>
                        membership.PlanName,
                    timeline.PlanName)
                .Set(
                    membership =>
                        membership.AccountType,
                    timeline.AccountType)
                .Set(
                    membership =>
                        membership.BillingFrequency,
                    timeline.BillingFrequency)
                .Set(
                    membership =>
                        membership.MembershipStartedAtUtc,
                    timeline.MembershipStartedAtUtc)
                .Set(
                    membership =>
                        membership.MembershipExpiresAtUtc,
                    timeline.MembershipExpiresAtUtc)
                .Set(
                    membership =>
                        membership.UpdatedAtUtc,
                    timeline.UpdatedAtUtc);

        await _collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    public async Task DeleteByIdentityUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return;
        }

        await _collection.DeleteOneAsync(
            timeline =>
                timeline.IdentityUserId ==
                identityUserId,
            cancellationToken);
    }
}