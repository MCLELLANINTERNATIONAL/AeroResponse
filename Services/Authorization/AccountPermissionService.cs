using System.Security.Claims;
using AeroResponse.Data.Mongo.Accounts;

namespace AeroResponse.Services.Authorization;

public sealed class AccountPermissionService
{
    private readonly MongoUserAccountRepository
        _userAccounts;

    public AccountPermissionService(
        MongoUserAccountRepository userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<string>
        GetAccountTypeAsync(
            ClaimsPrincipal principal,
            CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return MongoUserAccount
                .DefaultAccountType;
        }

        var userId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            return MongoUserAccount
                .DefaultAccountType;
        }

        try
        {
            var account =
                await _userAccounts
                    .FindByIdentityUserIdAsync(
                        userId,
                        cancellationToken);

            return NormalizeAccountType(
                account?.AccountType);
        }
        catch
        {
            return MongoUserAccount
                .DefaultAccountType;
        }
    }

    public async Task<bool>
        HasPermissionAsync(
            ClaimsPrincipal principal,
            string permission,
            CancellationToken cancellationToken = default)
    {
        var accountType =
            await GetAccountTypeAsync(
                principal,
                cancellationToken);

        return HasPermission(
            accountType,
            permission);
    }

    public static bool HasPermission(
        string? accountType,
        string permission)
    {
        var normalized =
            NormalizeAccountType(
                accountType);

        return permission switch
        {
            // Pilot reports + simulation
            AccountPermissions.PilotPages =>
                normalized is
                    "pilot"
                    or "trainer"
                    or "owner"
                    or "owner_small"
                    or "owner_large"
                    or "admin",

            // Instructor / trainer reports
            AccountPermissions.TrainerReports =>
                normalized is
                    "trainer"
                    or "owner"
                    or "owner_small"
                    or "owner_large"
                    or "admin",

            // Admin report / aircraft / scenarios
            AccountPermissions.AdminPages =>
                normalized == "admin",

            _ => false
        };
    }

    public static string NormalizeAccountType(
        string? accountType)
    {
        return accountType?
            .Trim()
            .ToLowerInvariant() switch
        {
            "pilot" =>
                "pilot",

            "trainer" =>
                "trainer",

            "owner" =>
                "owner",

            "owner_small" =>
                "owner_small",

            "owner_large" =>
                "owner_large",

            "admin" =>
                "admin",

            _ =>
                MongoUserAccount
                    .DefaultAccountType
        };
    }
}