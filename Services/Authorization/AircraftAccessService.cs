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

        // =================================================
        // ADMIN
        // =================================================
        //
        // Admin can see and use every aircraft.
        // =================================================

        if (accountType == "admin")
        {
            return AircraftAccessTier.All;
        }

        // =================================================
        // LARGE COMMERCIAL OWNER
        // =================================================

        if (accountType == "owner_large")
        {
            return AircraftAccessTier.LargeCommercial;
        }

        // =================================================
        // SMALL COMMERCIAL OWNER
        // =================================================

        if (accountType == "owner_small")
        {
            return AircraftAccessTier.SmallCommercial;
        }

        // =================================================
        // COMPANY MEMBERS
        // =================================================
        //
        // Pilots and trainers inherit the aircraft tier
        // from the company owner they are attached to.
        // =================================================

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
                return AircraftAccessTier.LargeCommercial;
            }

            if (ownerType == "owner_small")
            {
                return AircraftAccessTier.SmallCommercial;
            }
        }

        // =================================================
        // STANDALONE PILOT
        // =================================================
        //
        // No company:
        //
        // Cessna 172
        // Gulfstream G700
        // =================================================

        return AircraftAccessTier.Basic;
    }

    private static bool IsAircraftAllowed(
        Aircraft aircraft,
        AircraftAccessTier tier)
    {
        if (tier == AircraftAccessTier.None)
        {
            return false;
        }

        if (tier == AircraftAccessTier.All)
        {
            return true;
        }

        var name =
            aircraft.Name.Trim();

        // =================================================
        // STANDALONE PILOT
        // =================================================

        if (tier == AircraftAccessTier.Basic)
        {
            return
                name.Equals(
                    "Cessna 172",
                    StringComparison.OrdinalIgnoreCase)
                ||
                name.Equals(
                    "Gulfstream G700",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =================================================
        // SMALL COMMERCIAL
        // =================================================
        //
        // IMPORTANT:
        //
        // Small Commercial no longer inherits the
        // standalone pilot aircraft.
        //
        // It gets ONLY:
        //
        // ATR 72-600
        // De Havilland Dash 8 Q400
        // =================================================

        if (tier == AircraftAccessTier.SmallCommercial)
        {
            return
                name.Equals(
                    "ATR 72-600",
                    StringComparison.OrdinalIgnoreCase)
                ||
                name.Equals(
                    "De Havilland Dash 8 Q400",
                    StringComparison.OrdinalIgnoreCase);
        }

        // =================================================
        // LARGE COMMERCIAL
        // =================================================
        //
        // Large Commercial gets only the aircraft which
        // are unique to that tier.
        //
        // Put the exact Large Commercial aircraft names
        // from your Aircraft table in this block.
        // =================================================

        if (tier == AircraftAccessTier.LargeCommercial)
        {
            return IsLargeCommercialAircraft(
                name);
        }

        return false;
    }

    private static bool IsLargeCommercialAircraft(
        string aircraftName)
    {
        /*
         * Replace / extend these names with the exact
         * Large Commercial-only aircraft currently in your
         * database.
         *
         * Do NOT include:
         *
         * Cessna 172
         * Gulfstream G700
         * ATR 72-600
         * De Havilland Dash 8 Q400
         */

        return
            aircraftName.Equals(
                "Boeing 737",
                StringComparison.OrdinalIgnoreCase)
            ||
            aircraftName.Equals(
                "Airbus A320",
                StringComparison.OrdinalIgnoreCase)
            ||
            aircraftName.Equals(
                "Boeing 787",
                StringComparison.OrdinalIgnoreCase)
            ||
            aircraftName.Equals(
                "Airbus A350",
                StringComparison.OrdinalIgnoreCase);
    }
}

public enum AircraftAccessTier
{
    None,
    Basic,
    SmallCommercial,
    LargeCommercial,
    All
}