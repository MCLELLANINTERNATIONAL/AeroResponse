using Microsoft.AspNetCore.Authorization;

namespace AeroResponse.Services.Authorization;

public sealed class AccountPermissionRequirement
    : IAuthorizationRequirement
{
    public AccountPermissionRequirement(
        string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}