namespace AeroResponse.Services;

public static class CompanyMemberLimits
{
    public const int SmallPilotLimit = 20;
    public const int SmallTrainerLimit = 5;

    public const int LargePilotLimit = 50;
    public const int LargeTrainerLimit = 15;

    public static int GetLimit(
        string? ownerAccountType,
        string? memberAccountType)
    {
        var ownerType =
            ownerAccountType?
                .Trim()
                .ToLowerInvariant();

        var memberType =
            memberAccountType?
                .Trim()
                .ToLowerInvariant();

        return (ownerType, memberType) switch
        {
            ("owner_small", "pilot") =>
                SmallPilotLimit,

            ("owner_small", "trainer") =>
                SmallTrainerLimit,

            ("owner_large", "pilot") =>
                LargePilotLimit,

            ("owner_large", "trainer") =>
                LargeTrainerLimit,

            _ =>
                0
        };
    }

    public static bool IsSupportedMemberType(
        string? accountType)
    {
        return accountType?
            .Trim()
            .ToLowerInvariant() is
                "pilot" or
                "trainer";
    }
}