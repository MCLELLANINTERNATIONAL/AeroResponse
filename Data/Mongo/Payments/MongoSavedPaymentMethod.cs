using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AeroResponse.Data.Mongo.Payments;

public sealed class MongoSavedPaymentMethod
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("identityUserId")]
    public string IdentityUserId { get; set; } =
        string.Empty;

    /*
     * Demonstration token only.
     *
     * A real application would receive this token
     * from Stripe, PayPal, Adyen or another payment
     * provider.
     */
    [BsonElement("paymentToken")]
    public string PaymentToken { get; set; } =
        string.Empty;

    [BsonElement("cardBrand")]
    public string CardBrand { get; set; } =
        string.Empty;

    [BsonElement("lastFour")]
    public string LastFour { get; set; } =
        string.Empty;

    [BsonElement("expiryDate")]
    public string ExpiryDate { get; set; } =
        string.Empty;

    [BsonElement("cardholderName")]
    public string CardholderName { get; set; } =
        string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } =
        string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } =
        string.Empty;

    [BsonElement("addressLineOne")]
    public string AddressLineOne { get; set; } =
        string.Empty;

    [BsonElement("city")]
    public string City { get; set; } =
        string.Empty;

    [BsonElement("region")]
    public string Region { get; set; } =
        string.Empty;

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}