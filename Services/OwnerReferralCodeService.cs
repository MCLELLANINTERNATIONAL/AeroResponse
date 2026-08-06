using System.Security.Cryptography;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Data.Mongo.Referrals;
using MongoDB.Driver;

namespace AeroResponse.Services;

public sealed class OwnerReferralCodeService
{
    private const int CodeLength = 10;

    private const string CodeCharacters =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly
        MongoOwnerReferralCodeRepository _referralCodes;

    private readonly
        MongoUserAccountRepository _userAccounts;

    private readonly ILogger<OwnerReferralCodeService> _logger;

    public OwnerReferralCodeService(
        MongoOwnerReferralCodeRepository referralCodes,
        MongoUserAccountRepository userAccounts,
        ILogger<OwnerReferralCodeService> logger)
    {
        _referralCodes = referralCodes;
        _userAccounts = userAccounts;
        _logger = logger;
    }

    public async Task<OwnerReferralCodes>
        GetOrCreateCodesAsync(
            string ownerIdentityUserId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            ownerIdentityUserId))
        {
            throw new ArgumentException(
                "The owner Identity user ID is required.",
                nameof(ownerIdentityUserId));
        }

        var owner =
            await _userAccounts
                .FindByIdentityUserIdAsync(
                    ownerIdentityUserId,
                    cancellationToken);

        if (owner is null ||
            !IsOwnerAccount(owner.AccountType))
        {
            throw new InvalidOperationException(
                "Referral codes can only be created " +
                "for owner accounts.");
        }

        var utcNow =
            DateTime.UtcNow;

        var pilotCode =
            await GetOrCreateCodeAsync(
                ownerIdentityUserId,
                MongoOwnerReferralCode.PilotRole,
                utcNow,
                cancellationToken);

        var trainerCode =
            await GetOrCreateCodeAsync(
                ownerIdentityUserId,
                MongoOwnerReferralCode.TrainerRole,
                utcNow,
                cancellationToken);

        var expiry =
            pilotCode.ExpiresAtUtc <
            trainerCode.ExpiresAtUtc
                ? pilotCode.ExpiresAtUtc
                : trainerCode.ExpiresAtUtc;

        return new OwnerReferralCodes(
            pilotCode.Code,
            trainerCode.Code,
            expiry);
    }

    public async Task<ReferralCodeResolution?>
        ResolveCodeAsync(
            string? suppliedCode,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            suppliedCode))
        {
            return null;
        }

        var referralCode =
            await _referralCodes
                .FindValidByCodeAsync(
                    suppliedCode,
                    DateTime.UtcNow,
                    cancellationToken);

        if (referralCode is null)
        {
            return null;
        }

        var owner =
            await _userAccounts
                .FindByIdentityUserIdAsync(
                    referralCode.OwnerIdentityUserId,
                    cancellationToken);

        if (owner is null ||
            !IsOwnerAccount(owner.AccountType))
        {
            _logger.LogWarning(
                "Referral code {Code} references an invalid " +
                "owner account {OwnerId}.",
                referralCode.Code,
                referralCode.OwnerIdentityUserId);

            return null;
        }

        var accountType =
            referralCode.Role switch
            {
                MongoOwnerReferralCode.PilotRole =>
                    "pilot",

                MongoOwnerReferralCode.TrainerRole =>
                    "trainer",

                _ => null
            };

        return accountType is null
            ? null
            : new ReferralCodeResolution(
                referralCode.OwnerIdentityUserId,
                accountType);
    }

    private async Task<MongoOwnerReferralCode>
        GetOrCreateCodeAsync(
            string ownerIdentityUserId,
            string role,
            DateTime utcNow,
            CancellationToken cancellationToken)
    {
        var existing =
            await _referralCodes
                .FindForOwnerAndRoleAsync(
                    ownerIdentityUserId,
                    role,
                    cancellationToken);

        if (existing is not null &&
            existing.ExpiresAtUtc > utcNow)
        {
            return existing;
        }

        /*
         * Codes last exactly 24 hours from the time they
         * are generated.
         */
        var replacement =
            new MongoOwnerReferralCode
            {
                OwnerIdentityUserId =
                    ownerIdentityUserId,

                Role =
                    role,

                CreatedAtUtc =
                    utcNow,

                ExpiresAtUtc =
                    utcNow.AddHours(24)
            };

        /*
         * A unique index protects against the extremely
         * unlikely event of two owners receiving the same
         * randomly generated code.
         */
        for (var attempt = 0;
             attempt < 10;
             attempt++)
        {
            replacement.Code =
                CreateCode(role);

            try
            {
                await _referralCodes.UpsertAsync(
                    replacement,
                    cancellationToken);

                return replacement;
            }
            catch (MongoWriteException exception)
                when (
                    exception.WriteError?.Category ==
                    ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning(
                    "Generated duplicate referral code on " +
                    "attempt {Attempt}.",
                    attempt + 1);
            }
        }

        throw new InvalidOperationException(
            "A unique referral code could not be generated.");
    }

    private static string CreateCode(
        string role)
    {
        Span<char> generatedCharacters =
            stackalloc char[CodeLength];

        for (var index = 0;
             index < generatedCharacters.Length;
             index++)
        {
            generatedCharacters[index] =
                CodeCharacters[
                    RandomNumberGenerator.GetInt32(
                        CodeCharacters.Length)];
        }

        var prefix =
            role == MongoOwnerReferralCode.TrainerRole
                ? "T"
                : "P";

        return
            $"{prefix}-{new string(generatedCharacters)}";
    }

    private static bool IsOwnerAccount(
        string? accountType)
    {
        return accountType?
            .Trim()
            .ToLowerInvariant() is
                "owner_small" or
                "owner_large";
    }
}