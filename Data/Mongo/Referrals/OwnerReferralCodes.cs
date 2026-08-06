namespace AeroResponse.Data.Mongo.Referrals;

public sealed record OwnerReferralCodes(
    string PilotCode,
    string TrainerCode,
    DateTime ExpiresAtUtc);