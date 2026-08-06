using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Accounts;

public sealed class MongoUserAccount
{
    public const string DefaultAccountType =
        "guest";

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("identityUserId")]
    public string IdentityUserId { get; set; } =
        string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } =
        string.Empty;

    [BsonElement("surname")]
    public string Surname { get; set; } =
        string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } =
        string.Empty;

    [BsonElement("normalizedEmail")]
    public string NormalizedEmail { get; set; } =
        string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } =
        string.Empty;

    [BsonElement("account_type")]
    public string AccountType { get; set; } =
        DefaultAccountType;

    [BsonElement("businessName")]
    [BsonIgnoreIfNull]
    public string? BusinessName { get; set; }

    [BsonElement("ownerIdentityUserId")]
    [BsonIgnoreIfNull]
    public string? OwnerIdentityUserId { get; set; }

    [BsonElement("referralCodeUsed")]
    [BsonIgnoreIfNull]
    public string? ReferralCodeUsed { get; set; }

    /*
     * These counters are stored on owner accounts.
     *
     * They allow registration capacity to be reserved
     * atomically, preventing two simultaneous signups
     * from both claiming the final available seat.
     */
    [BsonElement("linkedPilotCount")]
    public int LinkedPilotCount { get; set; }

    [BsonElement("linkedTrainerCount")]
    public int LinkedTrainerCount { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}