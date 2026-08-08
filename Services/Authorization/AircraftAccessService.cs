using System.Security.Claims;
using AeroResponse.Data.Mongo.Accounts;
using AeroResponse.Models;

namespace AeroResponse.Services.Authorization;

public sealed class AircraftAccessService
{
    private readonly MongoUserAccountRepository _userAccounts;

    public AircraftAccessService(
        MongoUserAccountRepository userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<IReadOnlyList<Aircraft>>
        FilterAllowedAircraftAsync(
            ClaimsPrincipal principal,
            IEnumerable<Aircraft> aircraft,
            CancellationToken cancellationToken = default)
    {
        var tier =
            await GetAircraftAccessTierAsync(
                principal,
                cancellationToken);

        return aircraft
            .Where(item =>
                IsAircraftAllowed(
                    item,
                    tier))
            .ToArray();
    }

    public async Task<bool>
        CanUseAircraftAsync(
            ClaimsPrincipal principal,
            Aircraft aircraft,
            CancellationToken cancellationToken = default)
    {
        var tier =
            await GetAircraftAccessTierAsync(
                principal,
                cancellationToken);

        return IsAircraftAllowed(
            aircraft,
            tier);
    }

    public async Task<AircraftAccessTier>
        GetAircraftAccessTierAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return AircraftAccessTier.None;
        }

        var userId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return AircraftAccessTier.None;
        }

        var account =
            await _userAccounts
                .FindByIdentityUserIdAsync(
                    userId,
                    cancellationToken);

        if (account is null)
        {
            return AircraftAccessTier.None;
        }

        var accountType =
            AccountPermissionService
                .NormalizeAccountType(
                    account.AccountType);

        // -------------------------------------------------
        // ADMIN
        //
        // Administrators can use every aircraft.
        // -------------------------------------------------

        if (accountType == "admin")
        {
            return AircraftAccessTier.All;
        }

        // -------------------------------------------------
        // LARGE COMMERCIAL OWNER
        //
        // Large commercial owners can use every aircraft.
        // -------------------------------------------------

        if (accountType == "owner_large")
        {
            return AircraftAccessTier.All;
        }

        // -------------------------------------------------
        // SMALL COMMERCIAL OWNER
        //
        // Small commercial owners get:
        //
        // Cessna
        // Gulfstream
        // De Havilland
        // ATR
        // -------------------------------------------------

        if (accountType == "owner_small")
        {
            return AircraftAccessTier.SmallCommercial;
        }

        // -------------------------------------------------
        // COMPANY MEMBER
        //
        // Pilots and trainers inherit the aircraft tier
        // of the owner/company they are linked to.
        // -------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                account.OwnerIdentityUserId))
        {
            var owner =
                await _userAccounts
                    .FindByIdentityUserIdAsync(
                        account.OwnerIdentityUserId,
                        cancellationToken);

            var ownerType =
                AccountPermissionService
                    .NormalizeAccountType(
                        owner?.AccountType);

            if (ownerType == "owner_large")
            {
                return AircraftAccessTier.All;
            }

            if (ownerType == "owner_small")
            {
                return AircraftAccessTier.SmallCommercial;
            }
        }

        // -------------------------------------------------
        // STANDALONE PILOT / TRAINER
        //
        // No linked company.
        //
        // Only:
        // Cessna 172
        // Gulfstream G700
        // -------------------------------------------------

        return AircraftAccessTier.Basic;
    }

    private static bool IsAircraftAllowed(
        Aircraft aircraft,
        AircraftAccessTier tier)
    {
        if (tier == AircraftAccessTier.All)
        {
            return true;
        }

        if (tier == AircraftAccessTier.None)
        {
            return false;
        }

        var name =
            aircraft.Name.Trim();

        // Base aircraft available to pilots.
        if (name.Equals(
                "Cessna 172",
                StringComparison.OrdinalIgnoreCase) ||
            name.Equals(
                "Gulfstream G700",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Additional aircraft for small-commercial
        // companies and their members.
        if (tier == AircraftAccessTier.SmallCommercial)
        {
            if (name.Equals(
                    "De Havilland Dash 8 Q400",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Equals(
                    "ATR 72-600",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public enum AircraftAccessTier
{
    None,
    Basic,
    SmallCommercial,
    All
}