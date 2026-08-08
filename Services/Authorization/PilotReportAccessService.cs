using System.Security.Claims;
using AeroResponse.Data.Mongo.Accounts;

namespace AeroResponse.Services.Authorization;

public sealed class PilotReportAccessService
{
    private readonly MongoUserAccountRepository
        _userAccounts;

    public PilotReportAccessService(
        MongoUserAccountRepository userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<string?>
        ResolvePilotUserIdAsync(
            ClaimsPrincipal principal,
            string? requestedPilotUserId,
            CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var currentUserId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                currentUserId))
        {
            return null;
        }

        var currentAccount =
            await _userAccounts
                .FindByIdentityUserIdAsync(
                    currentUserId,
                    cancellationToken);

        if (currentAccount is null)
        {
            return null;
        }

        var accountType =
            AccountPermissionService
                .NormalizeAccountType(
                    currentAccount.AccountType);

        // =================================================
        // PILOT
        //
        // A pilot can only see their own reports.
        // =================================================

        if (accountType == "pilot")
        {
            if (string.IsNullOrWhiteSpace(
                    requestedPilotUserId))
            {
                return currentUserId;
            }

            return string.Equals(
                    requestedPilotUserId,
                    currentUserId,
                    StringComparison.OrdinalIgnoreCase)
                ? currentUserId
                : null;
        }

        // =================================================
        // ADMIN
        //
        // Admin can inspect any pilot.
        // =================================================

        if (accountType == "admin")
        {
            if (string.IsNullOrWhiteSpace(
                    requestedPilotUserId))
            {
                return null;
            }

            var requestedAccount =
                await _userAccounts
                    .FindByIdentityUserIdAsync(
                        requestedPilotUserId,
                        cancellationToken);

            if (requestedAccount is null)
            {
                return null;
            }

            var requestedType =
                AccountPermissionService
                    .NormalizeAccountType(
                        requestedAccount.AccountType);

            return requestedType == "pilot"
                ? requestedAccount.IdentityUserId
                : null;
        }

        // =================================================
        // TRAINER / OWNER
        //
        // Work out which company owner controls access.
        // =================================================

        string? ownerIdentityUserId =
            accountType switch
            {
                "trainer" =>
                    currentAccount
                        .OwnerIdentityUserId,

                "owner" =>
                    currentUserId,

                "owner_small" =>
                    currentUserId,

                "owner_large" =>
                    currentUserId,

                _ =>
                    null
            };

        if (string.IsNullOrWhiteSpace(
                ownerIdentityUserId))
        {
            return null;
        }

        // =================================================
        // LOAD ALL MEMBERS OF THAT COMPANY
        // =================================================

        var linkedMembers =
            await _userAccounts
                .FindLinkedMembersAsync(
                    ownerIdentityUserId,
                    cancellationToken);

        var linkedPilots =
            linkedMembers
                .Where(member =>
                    AccountPermissionService
                        .NormalizeAccountType(
                            member.AccountType)
                    == "pilot")
                .Where(member =>
                    !string.IsNullOrWhiteSpace(
                        member.IdentityUserId))
                .ToArray();

        // =================================================
        // NO PILOT REQUESTED
        //
        // Default to first linked pilot.
        // =================================================

        if (string.IsNullOrWhiteSpace(
                requestedPilotUserId))
        {
            return linkedPilots
                .FirstOrDefault()
                ?.IdentityUserId;
        }

        // =================================================
        // SPECIFIC PILOT REQUESTED
        //
        // Ensure that pilot belongs to this company.
        // =================================================

        var allowed =
            linkedPilots.Any(
                pilot =>
                    string.Equals(
                        pilot.IdentityUserId,
                        requestedPilotUserId,
                        StringComparison.OrdinalIgnoreCase));

        return allowed
            ? requestedPilotUserId
            : null;
    }
}