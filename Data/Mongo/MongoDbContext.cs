using MongoDB.Driver;

namespace AeroResponse.Data.Mongo;


public sealed class MongoDbContext
{

    public MongoDbContext(
        IMongoClient client,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(
                settings.DatabaseName))
        {
            throw new InvalidOperationException(
                "The MongoDB database name is not configured.");
        }

        Client = client;
        Database = client.GetDatabase(
            settings.DatabaseName);
    }


    public IMongoClient Client { get; }

    public IMongoDatabase Database { get; }


    public IMongoCollection<TDocument> GetCollection<TDocument>(
        string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException(
                "A MongoDB collection name is required.",
                nameof(collectionName));
        }

        return Database.GetCollection<TDocument>(
            collectionName);
    }
}