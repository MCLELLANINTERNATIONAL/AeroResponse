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
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return null;
        }

        return await _collection
            .Find(
                method =>
                    method.IdentityUserId == identityUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        MongoSavedPaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);

        var filter =
            Builders<MongoSavedPaymentMethod>
                .Filter
                .Eq(
                    method => method.IdentityUserId,
                    paymentMethod.IdentityUserId);

        await _collection.ReplaceOneAsync(
            filter,
            paymentMethod,
            new ReplaceOptions
            {
                IsUpsert = true
            },
            cancellationToken);
    }
}