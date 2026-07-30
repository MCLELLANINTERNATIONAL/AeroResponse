using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Accounts;

public sealed class MongoUserAccount
{
    public const string DefaultAccountType = "guest";

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

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}