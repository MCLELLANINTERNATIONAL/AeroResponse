using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Memberships;

public sealed class MongoMemberTimeline
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? Id { get; set; }

    [BsonElement("identityUserId")]
    public string IdentityUserId { get; set; } =
        string.Empty;

    [BsonElement("planName")]
    public string PlanName { get; set; } =
        string.Empty;

    [BsonElement("accountType")]
    public string AccountType { get; set; } =
        string.Empty;

    [BsonElement("billingFrequency")]
    public string BillingFrequency { get; set; } =
        string.Empty;

    [BsonElement("membershipStartedAtUtc")]
    public DateTime MembershipStartedAtUtc { get; set; }

    [BsonElement("membershipExpiresAtUtc")]
    public DateTime MembershipExpiresAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}
