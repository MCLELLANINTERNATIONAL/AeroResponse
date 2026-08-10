using AeroResponse.Services;
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

        var ownerMemberIndex =
            new CreateIndexModel<MongoUserAccount>(
                Builders<MongoUserAccount>
                    .IndexKeys
                    .Ascending(
                        account =>
                            account.OwnerIdentityUserId)
                    .Ascending(
                        account =>
                            account.AccountType),
                new CreateIndexOptions
                {
                    Name =
                        "ix_userAccounts_owner_accountType"
                });

        await _accounts.Indexes.CreateManyAsync(
            new[]
            {
                identityUserIdIndex,
                normalizedEmailIndex,
                ownerMemberIndex
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
        if (string.IsNullOrWhiteSpace(identityUserId))
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

    public async Task<IReadOnlyList<CompanyMemberSummary>>
        FindAllPilotsAsync(
            CancellationToken cancellationToken = default)
    {
        var accounts =
            await _accounts
                .Find(account => account.AccountType == "pilot")
                .SortBy(account => account.Surname)
                .ThenBy(account => account.FirstName)
                .ToListAsync(cancellationToken);

        return accounts
            .Select(account =>
                new CompanyMemberSummary(
                    account.IdentityUserId,
                    account.FirstName,
                    account.Surname,
                    account.Email,
                    account.AccountType,
                    account.CreatedAtUtc))
            .ToArray();
    }

    public async Task<IReadOnlyList<CompanyMemberSummary>>
        FindLinkedMembersAsync(
            string ownerIdentityUserId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId))
        {
            return Array.Empty<CompanyMemberSummary>();
        }

        var filter =
            Builders<MongoUserAccount>
                .Filter
                .And(
                    Builders<MongoUserAccount>
                        .Filter
                        .Eq(
                            account =>
                                account.OwnerIdentityUserId,
                            ownerIdentityUserId),

                    Builders<MongoUserAccount>
                        .Filter
                        .In(
                            account =>
                                account.AccountType,
                            new[]
                            {
                                "pilot",
                                "trainer"
                            }));

        var accounts =
            await _accounts
                .Find(filter)
                .SortBy(
                    account =>
                        account.AccountType)
                .ThenBy(
                    account =>
                        account.Surname)
                .ThenBy(
                    account =>
                        account.FirstName)
                .ToListAsync(
                    cancellationToken);

        return accounts
            .Select(
                account =>
                    new CompanyMemberSummary(
                        account.IdentityUserId,
                        account.FirstName,
                        account.Surname,
                        account.Email,
                        account.AccountType,
                        account.CreatedAtUtc))
            .ToArray();
    }

    public async Task<long> CountLinkedMembersAsync(
        string ownerIdentityUserId,
        string memberAccountType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId) ||
            !CompanyMemberLimits.IsSupportedMemberType(
                memberAccountType))
        {
            return 0;
        }

        var normalizedMemberType =
            memberAccountType
                .Trim()
                .ToLowerInvariant();

        return await _accounts.CountDocumentsAsync(
            account =>
                account.OwnerIdentityUserId ==
                    ownerIdentityUserId &&
                account.AccountType ==
                    normalizedMemberType,
            cancellationToken:
                cancellationToken);
    }

    public async Task UpdateAccountTypeAsync(
        string identityUserId,
        string accountType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(identityUserId));
        }

        if (string.IsNullOrWhiteSpace(accountType))
        {
            throw new ArgumentException(
                "The account type is required.",
                nameof(accountType));
        }

        var normalizedAccountType =
            accountType
                .Trim()
                .ToLowerInvariant();

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
                    normalizedAccountType);

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

    public async Task UpdateProfileAsync(
        string identityUserId,
        string firstName,
        string surname,
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(identityUserId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "The first name is required.",
                nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(surname))
        {
            throw new ArgumentException(
                "The surname is required.",
                nameof(surname));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "The email address is required.",
                nameof(email));
        }

        var cleanedFirstName =
            firstName.Trim();

        var cleanedSurname =
            surname.Trim();

        var cleanedEmail =
            email.Trim();

        var normalizedEmail =
            cleanedEmail.ToUpperInvariant();

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
                        account.FirstName,
                    cleanedFirstName)
                .Set(
                    account =>
                        account.Surname,
                    cleanedSurname)
                .Set(
                    account =>
                        account.Email,
                    cleanedEmail)
                .Set(
                    account =>
                        account.NormalizedEmail,
                    normalizedEmail);

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

    public async Task UpdateBusinessNameAsync(
        string identityUserId,
        string businessName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(identityUserId));
        }

        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException(
                "The business name is required.",
                nameof(businessName));
        }

        var cleanedBusinessName =
            businessName.Trim();

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
                        account.BusinessName,
                    cleanedBusinessName);

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

    public async Task<bool> TryReserveCompanySeatAsync(
        string ownerIdentityUserId,
        string memberAccountType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId) ||
            !CompanyMemberLimits.IsSupportedMemberType(
                memberAccountType))
        {
            return false;
        }

        var normalizedMemberType =
            memberAccountType
                .Trim()
                .ToLowerInvariant();

        var owner =
            await FindByIdentityUserIdAsync(
                ownerIdentityUserId,
                cancellationToken);

        if (owner is null)
        {
            return false;
        }

        var capacityLimit =
            CompanyMemberLimits.GetLimit(
                owner.AccountType,
                normalizedMemberType);

        if (capacityLimit <= 0)
        {
            return false;
        }

        /*
         * Count the real linked documents first.
         *
         * This detects accounts that were manually imported
         * into MongoDB and therefore did not increment the
         * owner's stored seat counter.
         */
        var actualLinkedCount =
            await CountLinkedMembersAsync(
                ownerIdentityUserId,
                normalizedMemberType,
                cancellationToken);

        /*
         * Ensure the cached counter is never lower than the
         * number of real linked accounts.
         *
         * $max is important here. Unlike a normal Set, it
         * cannot lower a counter that another simultaneous
         * registration has already increased.
         */
        await EnsureMinimumCompanyMemberCountAsync(
            ownerIdentityUserId,
            normalizedMemberType,
            actualLinkedCount,
            cancellationToken);

        if (actualLinkedCount >= capacityLimit)
        {
            return false;
        }

        var ownerFilter =
            Builders<MongoUserAccount>
                .Filter
                .And(
                    Builders<MongoUserAccount>
                        .Filter
                        .Eq(
                            account =>
                                account.IdentityUserId,
                            ownerIdentityUserId),

                    Builders<MongoUserAccount>
                        .Filter
                        .In(
                            account =>
                                account.AccountType,
                            new[]
                            {
                                "owner_small",
                                "owner_large"
                            }));

        FilterDefinition<MongoUserAccount>
            capacityFilter;

        UpdateDefinition<MongoUserAccount>
            reserveUpdate;

        if (normalizedMemberType == "pilot")
        {
            capacityFilter =
                Builders<MongoUserAccount>
                    .Filter
                    .Lt(
                        account =>
                            account.LinkedPilotCount,
                        capacityLimit);

            reserveUpdate =
                Builders<MongoUserAccount>
                    .Update
                    .Inc(
                        account =>
                            account.LinkedPilotCount,
                        1);
        }
        else
        {
            capacityFilter =
                Builders<MongoUserAccount>
                    .Filter
                    .Lt(
                        account =>
                            account.LinkedTrainerCount,
                        capacityLimit);

            reserveUpdate =
                Builders<MongoUserAccount>
                    .Update
                    .Inc(
                        account =>
                            account.LinkedTrainerCount,
                        1);
        }

        /*
         * This update is atomic.
         *
         * Only one registration can claim the final
         * available pilot or trainer seat.
         */
        var result =
            await _accounts.UpdateOneAsync(
                Builders<MongoUserAccount>
                    .Filter
                    .And(
                        ownerFilter,
                        capacityFilter),
                reserveUpdate,
                cancellationToken:
                    cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task ReleaseCompanySeatAsync(
        string ownerIdentityUserId,
        string memberAccountType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId) ||
            !CompanyMemberLimits.IsSupportedMemberType(
                memberAccountType))
        {
            return;
        }

        var normalizedMemberType =
            memberAccountType
                .Trim()
                .ToLowerInvariant();

        var ownerFilter =
            Builders<MongoUserAccount>
                .Filter
                .Eq(
                    account =>
                        account.IdentityUserId,
                    ownerIdentityUserId);

        FilterDefinition<MongoUserAccount>
            positiveCountFilter;

        UpdateDefinition<MongoUserAccount>
            releaseUpdate;

        if (normalizedMemberType == "pilot")
        {
            positiveCountFilter =
                Builders<MongoUserAccount>
                    .Filter
                    .Gt(
                        account =>
                            account.LinkedPilotCount,
                        0);

            releaseUpdate =
                Builders<MongoUserAccount>
                    .Update
                    .Inc(
                        account =>
                            account.LinkedPilotCount,
                        -1);
        }
        else
        {
            positiveCountFilter =
                Builders<MongoUserAccount>
                    .Filter
                    .Gt(
                        account =>
                            account.LinkedTrainerCount,
                        0);

            releaseUpdate =
                Builders<MongoUserAccount>
                    .Update
                    .Inc(
                        account =>
                            account.LinkedTrainerCount,
                        -1);
        }

        await _accounts.UpdateOneAsync(
            Builders<MongoUserAccount>
                .Filter
                .And(
                    ownerFilter,
                    positiveCountFilter),
            releaseUpdate,
            cancellationToken:
                cancellationToken);
    }

    public async Task<bool> DisconnectCompanyMemberAsync(
        string ownerIdentityUserId,
        string memberIdentityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId) ||
            string.IsNullOrWhiteSpace(
                memberIdentityUserId))
        {
            return false;
        }

        var member =
            await _accounts
                .Find(
                    account =>
                        account.IdentityUserId ==
                            memberIdentityUserId &&
                        account.OwnerIdentityUserId ==
                            ownerIdentityUserId &&
                        (
                            account.AccountType ==
                                "pilot" ||
                            account.AccountType ==
                                "trainer"
                        ))
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (member is null)
        {
            return false;
        }

        var memberFilter =
            Builders<MongoUserAccount>
                .Filter
                .And(
                    Builders<MongoUserAccount>
                        .Filter
                        .Eq(
                            account =>
                                account.IdentityUserId,
                            memberIdentityUserId),

                    Builders<MongoUserAccount>
                        .Filter
                        .Eq(
                            account =>
                                account.OwnerIdentityUserId,
                            ownerIdentityUserId),

                    Builders<MongoUserAccount>
                        .Filter
                        .Eq(
                            account =>
                                account.AccountType,
                            member.AccountType));

        var disconnectUpdate =
            Builders<MongoUserAccount>
                .Update
                .Set(
                    account =>
                        account.AccountType,
                    MongoUserAccount
                        .DefaultAccountType)
                .Unset(
                    account =>
                        account.OwnerIdentityUserId)
                .Unset(
                    account =>
                        account.ReferralCodeUsed);

        var result =
            await _accounts.UpdateOneAsync(
                memberFilter,
                disconnectUpdate,
                cancellationToken:
                    cancellationToken);

        if (result.ModifiedCount != 1)
        {
            return false;
        }

        await ReleaseCompanySeatAsync(
            ownerIdentityUserId,
            member.AccountType,
            cancellationToken);

        return true;
    }

    public async Task SynchronizeCompanyMemberCountsAsync(
        string ownerIdentityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId))
        {
            return;
        }

        var pilotCountTask =
            CountLinkedMembersAsync(
                ownerIdentityUserId,
                "pilot",
                cancellationToken);

        var trainerCountTask =
            CountLinkedMembersAsync(
                ownerIdentityUserId,
                "trainer",
                cancellationToken);

        await Task.WhenAll(
            pilotCountTask,
            trainerCountTask);

        var pilotCount =
            await pilotCountTask;

        var trainerCount =
            await trainerCountTask;

        var filter =
            Builders<MongoUserAccount>
                .Filter
                .Eq(
                    account =>
                        account.IdentityUserId,
                    ownerIdentityUserId);

        var update =
            Builders<MongoUserAccount>
                .Update
                .Set(
                    account =>
                        account.LinkedPilotCount,
                    ConvertCountToInt(pilotCount))
                .Set(
                    account =>
                        account.LinkedTrainerCount,
                    ConvertCountToInt(trainerCount));

        await _accounts.UpdateOneAsync(
            filter,
            update,
            cancellationToken:
                cancellationToken);
    }

    public async Task SynchronizeAllOwnerMemberCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var ownerFilter =
            Builders<MongoUserAccount>
                .Filter
                .In(
                    account =>
                        account.AccountType,
                    new[]
                    {
                        "owner_small",
                        "owner_large"
                    });

        var ownerIds =
            await _accounts
                .Find(ownerFilter)
                .Project(
                    account =>
                        account.IdentityUserId)
                .ToListAsync(
                    cancellationToken);

        foreach (var ownerIdentityUserId in ownerIds)
        {
            await SynchronizeCompanyMemberCountsAsync(
                ownerIdentityUserId,
                cancellationToken);
        }
    }

    private async Task
        EnsureMinimumCompanyMemberCountAsync(
            string ownerIdentityUserId,
            string memberAccountType,
            long actualLinkedCount,
            CancellationToken cancellationToken)
    {
        var safeCount =
            ConvertCountToInt(
                actualLinkedCount);

        var filter =
            Builders<MongoUserAccount>
                .Filter
                .Eq(
                    account =>
                        account.IdentityUserId,
                    ownerIdentityUserId);

        UpdateDefinition<MongoUserAccount> update;

        if (memberAccountType == "pilot")
        {
            update =
                Builders<MongoUserAccount>
                    .Update
                    .Max(
                        account =>
                            account.LinkedPilotCount,
                        safeCount);
        }
        else
        {
            update =
                Builders<MongoUserAccount>
                    .Update
                    .Max(
                        account =>
                            account.LinkedTrainerCount,
                        safeCount);
        }

        await _accounts.UpdateOneAsync(
            filter,
            update,
            cancellationToken:
                cancellationToken);
    }

    private static int ConvertCountToInt(
        long count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return count >= int.MaxValue
            ? int.MaxValue
            : (int)count;
    }

    public async Task DeleteAccountAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return;
        }

        var account =
            await FindByIdentityUserIdAsync(
                identityUserId,
                cancellationToken);

        if (account is null)
        {
            return;
        }

        // If a pilot or trainer is connected to a company,
        // free the company's occupied seat.
        if (!string.IsNullOrWhiteSpace(
                account.OwnerIdentityUserId) &&
            CompanyMemberLimits.IsSupportedMemberType(
                account.AccountType))
        {
            await ReleaseCompanySeatAsync(
                account.OwnerIdentityUserId,
                account.AccountType,
                cancellationToken);
        }

        // If a company owner deletes their account,
        // disconnect linked members rather than deleting
        // those members' individual accounts.
        if (account.AccountType is
            "owner_small" or "owner_large")
        {
            var linkedMembersFilter =
                Builders<MongoUserAccount>
                    .Filter
                    .Eq(
                        member =>
                            member.OwnerIdentityUserId,
                        identityUserId);

            var disconnectUpdate =
                Builders<MongoUserAccount>
                    .Update
                    .Unset(
                        member =>
                            member.OwnerIdentityUserId)
                    .Unset(
                        member =>
                            member.ReferralCodeUsed);

            await _accounts.UpdateManyAsync(
                linkedMembersFilter,
                disconnectUpdate,
                cancellationToken:
                    cancellationToken);
        }

        await _accounts.DeleteOneAsync(
            userAccount =>
                userAccount.IdentityUserId ==
                identityUserId,
            cancellationToken);
    }
}