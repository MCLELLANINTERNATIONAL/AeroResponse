using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Accounts;

public sealed class MongoUserAccountRepository
{
    private const string CollectionName =
        "userAccounts";

    private readonly
        IMongoCollection<MongoUserAccount> _accounts;

    public MongoUserAccountRepository(
        MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _accounts =
            context.GetCollection<MongoUserAccount>(
                CollectionName);
    }

    public async Task EnsureIndexesAsync(
        CancellationToken cancellationToken = default)
    {
        var identityUserIdIndex =
            new CreateIndexModel<MongoUserAccount>(
                Builders<MongoUserAccount>
                    .IndexKeys
                    .Ascending(
                        account =>
                            account.IdentityUserId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name =
                        "ux_userAccounts_identityUserId"
                });

        var normalizedEmailIndex =
            new CreateIndexModel<MongoUserAccount>(
                Builders<MongoUserAccount>
                    .IndexKeys
                    .Ascending(
                        account =>
                            account.NormalizedEmail),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name =
                        "ux_userAccounts_normalizedEmail"
                });

        await _accounts.Indexes.CreateManyAsync(
            new[]
            {
                identityUserIdIndex,
                normalizedEmailIndex
            },
            cancellationToken);
    }

    public async Task CreateAsync(
        MongoUserAccount account,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await _accounts.InsertOneAsync(
            account,
            cancellationToken:
                cancellationToken);
    }

    public async Task<MongoUserAccount?>
        FindByIdentityUserIdAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            identityUserId))
        {
            return null;
        }

        return await _accounts
            .Find(
                account =>
                    account.IdentityUserId ==
                    identityUserId)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task UpdateAccountTypeAsync(
        string identityUserId,
        string accountType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            identityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(identityUserId));
        }

        if (string.IsNullOrWhiteSpace(
            accountType))
        {
            throw new ArgumentException(
                "The account type is required.",
                nameof(accountType));
        }

        var filter =
            Builders<MongoUserAccount>
                .Filter
                .Eq(
                    account =>
                        account.IdentityUserId,
                    identityUserId);

        var update =
            Builders<MongoUserAccount>
                .Update
                .Set(
                    account =>
                        account.AccountType,
                    accountType);

        var result =
            await _accounts.UpdateOneAsync(
                filter,
                update,
                cancellationToken:
                    cancellationToken);

        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException(
                "The MongoDB user account could not be found.");
        }
    }
}