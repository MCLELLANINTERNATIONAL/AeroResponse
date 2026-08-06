namespace AeroResponse.Data.Mongo.Referrals;

public sealed record ReferralCodeResolution(
    string OwnerIdentityUserId,
    string AccountType);