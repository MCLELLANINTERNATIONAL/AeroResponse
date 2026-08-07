using MongoDB.Driver;

namespace AeroResponse.Data.Mongo.Payments;

public sealed class MongoSavedPaymentMethodRepository
{
    private const string CollectionName =
        "savedPaymentMethods";

    private readonly
        IMongoCollection<MongoSavedPaymentMethod> _collection;

    public MongoSavedPaymentMethodRepository(
        MongoDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _collection =
            context.GetCollection<MongoSavedPaymentMethod>(
                CollectionName);
    }

    public async Task<MongoSavedPaymentMethod?>
        FindByIdentityUserIdAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            identityUserId))
        {
            return null;
        }

        return await _collection
            .Find(
                payment =>
                    payment.IdentityUserId ==
                    identityUserId)
            .FirstOrDefaultAsync(
                cancellationToken);
    }

    public async Task UpsertAsync(
        MongoSavedPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            paymentMethod);

        if (string.IsNullOrWhiteSpace(
            paymentMethod.IdentityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(paymentMethod));
        }

        var filter =
            Builders<MongoSavedPaymentMethod>
                .Filter
                .Eq(
                    payment =>
                        payment.IdentityUserId,
                    paymentMethod.IdentityUserId);

        var update =
            Builders<MongoSavedPaymentMethod>
                .Update
                .SetOnInsert(
                    payment =>
                        payment.IdentityUserId,
                    paymentMethod.IdentityUserId)
                .Set(
                    payment =>
                        payment.PaymentToken,
                    paymentMethod.PaymentToken)
                .Set(
                    payment =>
                        payment.CardBrand,
                    paymentMethod.CardBrand)
                .Set(
                    payment =>
                        payment.LastFour,
                    paymentMethod.LastFour)
                .Set(
                    payment =>
                        payment.ExpiryDate,
                    paymentMethod.ExpiryDate)
                .Set(
                    payment =>
                        payment.CardholderName,
                    paymentMethod.CardholderName)
                .Set(
                    payment =>
                        payment.Country,
                    paymentMethod.Country)
                .Set(
                    payment =>
                        payment.PostalCode,
                    paymentMethod.PostalCode)
                .Set(
                    payment =>
                        payment.AddressLineOne,
                    paymentMethod.AddressLineOne)
                .Set(
                    payment =>
                        payment.City,
                    paymentMethod.City)
                .Set(
                    payment =>
                        payment.Region,
                    paymentMethod.Region)
                .Set(
                    payment =>
                        payment.UpdatedAtUtc,
                    paymentMethod.UpdatedAtUtc);

        await _collection.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }

    public async Task DeleteByIdentityUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
            identityUserId))
        {
            throw new ArgumentException(
                "The Identity user ID is required.",
                nameof(identityUserId));
        }

        var filter =
            Builders<MongoSavedPaymentMethod>
                .Filter
                .Eq(
                    payment =>
                        payment.IdentityUserId,
                    identityUserId);

        var result =
            await _collection.DeleteOneAsync(
                filter,
                cancellationToken);

        if (result.DeletedCount == 0)
        {
            throw new InvalidOperationException(
                "No saved payment method was found.");
        }
    }

    public async Task DeleteIfExistsByIdentityUserIdAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return;
        }

        await _collection.DeleteOneAsync(
            payment =>
                payment.IdentityUserId ==
                identityUserId,
            cancellationToken);
    }
}