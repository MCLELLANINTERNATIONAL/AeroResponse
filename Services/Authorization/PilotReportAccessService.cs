using System.Security.Claims;
using AeroResponse.Data.Mongo.Accounts;

namespace AeroResponse.Services.Authorization;

public sealed class PilotReportAccessService
{
    private readonly MongoUserAccountRepository _userAccounts;

    public PilotReportAccessService(MongoUserAccountRepository userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<string?> ResolvePilotUserIdAsync(
        ClaimsPrincipal principal,
        string? requestedPilotUserId,
        CancellationToken cancellationToken = default)
    {
        var context = await GetPilotSelectionContextAsync(principal, cancellationToken);
        if (context is null)
        {
            return null;
        }

        if (context.AccountType == "pilot")
        {
            if (string.IsNullOrWhiteSpace(requestedPilotUserId))
            {
                return context.CurrentUserId;
            }

            return string.Equals(
                requestedPilotUserId,
                context.CurrentUserId,
                StringComparison.OrdinalIgnoreCase)
                ? context.CurrentUserId
                : null;
        }

        var pilots = context.Pilots;
        if (string.IsNullOrWhiteSpace(requestedPilotUserId))
        {
            return pilots.FirstOrDefault()?.IdentityUserId;
        }

        return pilots.Any(pilot => string.Equals(
                pilot.IdentityUserId,
                requestedPilotUserId,
                StringComparison.OrdinalIgnoreCase))
            ? requestedPilotUserId
            : null;
    }

    public async Task<PilotReportSelectionContext?> GetPilotSelectionContextAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return null;
        }

        var currentAccount = await _userAccounts.FindByIdentityUserIdAsync(
            currentUserId,
            cancellationToken);
        if (currentAccount is null)
        {
            return null;
        }

        var accountType = AccountPermissionService.NormalizeAccountType(currentAccount.AccountType);

        if (accountType == "pilot")
        {
            return new PilotReportSelectionContext(
                currentUserId,
                accountType,
                Array.Empty<CompanyMemberSummary>());
        }

        if (accountType == "admin")
        {
            var allPilots = await _userAccounts.FindAllPilotsAsync(cancellationToken);
            return new PilotReportSelectionContext(currentUserId, accountType, allPilots);
        }

        string? ownerIdentityUserId = accountType switch
        {
            "trainer" => currentAccount.OwnerIdentityUserId,
            "owner" => currentUserId,
            "owner_small" => currentUserId,
            "owner_large" => currentUserId,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(ownerIdentityUserId))
        {
            return new PilotReportSelectionContext(
                currentUserId,
                accountType,
                Array.Empty<CompanyMemberSummary>());
        }

        var linkedMembers = await _userAccounts.FindLinkedMembersAsync(
            ownerIdentityUserId,
            cancellationToken);

        var linkedPilots = linkedMembers
            .Where(member =>
                AccountPermissionService.NormalizeAccountType(member.AccountType) == "pilot")
            .Where(member => !string.IsNullOrWhiteSpace(member.IdentityUserId))
            .OrderBy(member => member.Surname)
            .ThenBy(member => member.FirstName)
            .ToArray();

        return new PilotReportSelectionContext(currentUserId, accountType, linkedPilots);
    }
}

public sealed record PilotReportSelectionContext(
    string CurrentUserId,
    string AccountType,
    IReadOnlyList<CompanyMemberSummary> Pilots)
{
    public bool CanSelectPilot => AccountType is "trainer" or "admin";
}
