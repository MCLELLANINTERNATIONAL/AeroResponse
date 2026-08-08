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

        // =====================================================
        // PILOT
        // A pilot can only view their own reports.
        // =====================================================

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

        // =====================================================
        // ADMIN
        // An admin can view the report of any pilot.
        // =====================================================

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

            var requestedAccountType =
                AccountPermissionService
                    .NormalizeAccountType(
                        requestedAccount.AccountType);

            return requestedAccountType == "pilot"
                ? requestedAccount.IdentityUserId
                : null;
        }

        // =====================================================
        // OWNER / TRAINER
        //
        // Owners can inspect pilots linked to themselves.
        // Trainers can inspect pilots linked to the same owner.
        // =====================================================

        string? ownerIdentityUserId =
            accountType switch
            {
                "trainer" =>
                    currentAccount.OwnerIdentityUserId,

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

        var linkedMembers =
            await _userAccounts
                .FindLinkedMembersAsync(
                    ownerIdentityUserId,
                    cancellationToken);

        var linkedPilots =
            linkedMembers
                .Where(member =>
                    string.Equals(
                        member.AccountType,
                        "pilot",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        // No requested pilot:
        // use the first linked pilot if available.
        if (string.IsNullOrWhiteSpace(
                requestedPilotUserId))
        {
            return linkedPilots
                .FirstOrDefault()
                ?.IdentityUserId;
        }

        var pilotIsLinked =
            linkedPilots.Any(
                pilot =>
                    string.Equals(
                        pilot.IdentityUserId,
                        requestedPilotUserId,
                        StringComparison.OrdinalIgnoreCase));

        return pilotIsLinked
            ? requestedPilotUserId
            : null;
    }
}