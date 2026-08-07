using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Referrals;

public sealed class MongoOwnerReferralCodeRepository
{
    private const string CollectionName =
        "ownerReferralCodes";

    private readonly
        IMongoCollection<MongoOwnerReferralCode> _codes;

    public MongoOwnerReferralCodeRepository(
        MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _codes =
            context.GetCollection<MongoOwnerReferralCode>(
                CollectionName);
    }

    public async Task EnsureIndexesAsync(
        CancellationToken cancellationToken = default)
    {
        var uniqueCodeIndex =
            new CreateIndexModel<MongoOwnerReferralCode>(
                Builders<MongoOwnerReferralCode>
                    .IndexKeys
                    .Ascending(code => code.Code),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_ownerReferralCodes_code"
                });

        var ownerRoleIndex =
            new CreateIndexModel<MongoOwnerReferralCode>(
                Builders<MongoOwnerReferralCode>
                    .IndexKeys
                    .Ascending(code =>
                        code.OwnerIdentityUserId)
                    .Ascending(code =>
                        code.Role),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name =
                        "ux_ownerReferralCodes_owner_role"
                });

        var expiryIndex =
            new CreateIndexModel<MongoOwnerReferralCode>(
                Builders<MongoOwnerReferralCode>
                    .IndexKeys
                    .Ascending(code =>
                        code.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    ExpireAfter =
                        TimeSpan.Zero,

                    Name =
                        "ttl_ownerReferralCodes_expiry"
                });

        await _codes.Indexes.CreateManyAsync(
            new[]
            {
                uniqueCodeIndex,
                ownerRoleIndex,
                expiryIndex
            },
            cancellationToken);
    }

    public async Task<MongoOwnerReferralCode?>
        FindForOwnerAndRoleAsync(
            string ownerIdentityUserId,
            string role,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId) ||
            string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return await _codes
            .Find(code =>
                code.OwnerIdentityUserId ==
                    ownerIdentityUserId &&
                code.Role == role)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task<MongoOwnerReferralCode?>
        FindValidByCodeAsync(
            string code,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalisedCode =
            code.Trim().ToUpperInvariant();

        return await _codes
            .Find(referralCode =>
                referralCode.Code ==
                    normalisedCode &&
                referralCode.ExpiresAtUtc >
                    utcNow)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
        MongoOwnerReferralCode referralCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            referralCode);

        var filter =
            Builders<MongoOwnerReferralCode>
                .Filter
                .Where(code =>
                    code.OwnerIdentityUserId ==
                        referralCode.OwnerIdentityUserId &&
                    code.Role ==
                        referralCode.Role);

        var update =
            Builders<MongoOwnerReferralCode>
                .Update
                .Set(
                    code => code.Code,
                    referralCode.Code)
                .Set(
                    code => code.CreatedAtUtc,
                    referralCode.CreatedAtUtc)
                .Set(
                    code => code.ExpiresAtUtc,
                    referralCode.ExpiresAtUtc)
                .SetOnInsert(
                    code =>
                        code.OwnerIdentityUserId,
                    referralCode.OwnerIdentityUserId)
                .SetOnInsert(
                    code => code.Role,
                    referralCode.Role);

        await _codes.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    public async Task DeleteForOwnerAsync(
        string ownerIdentityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            ownerIdentityUserId))
        {
            return;
        }

        await _codes.DeleteManyAsync(
            code =>
                code.OwnerIdentityUserId ==
                ownerIdentityUserId,
            cancellationToken);
    }
}