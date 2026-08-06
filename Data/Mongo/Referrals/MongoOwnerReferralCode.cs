using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Referrals;

public sealed class MongoOwnerReferralCode
{
    public const string PilotRole = "pilot";
    public const string TrainerRole = "trainer";

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("ownerIdentityUserId")]
    public string OwnerIdentityUserId { get; set; } =
        string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } =
        string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } =
        string.Empty;

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}