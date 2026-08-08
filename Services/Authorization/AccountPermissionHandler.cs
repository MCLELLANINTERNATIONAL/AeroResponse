using Microsoft.AspNetCore.Authorization;

namespace AeroResponse.Services.Authorization;

public sealed class AccountPermissionHandler
    : AuthorizationHandler<
        AccountPermissionRequirement>
{
    private readonly AccountPermissionService
        _permissionService;

    public AccountPermissionHandler(
        AccountPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountPermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var allowed =
            await _permissionService
                .HasPermissionAsync(
                    context.User,
                    requirement.Permission);

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}