using MongoDB.Bson;

namespace AeroResponse.Data.Mongo;


public sealed class MongoConnectionProbe
{
    private readonly MongoDbContext _context;

    public MongoConnectionProbe(
        MongoDbContext context)
    {
        _context = context;
    }


    public async Task<bool> PingAsync(
        CancellationToken cancellationToken = default)
    {
        var command = new BsonDocument(
            "ping",
            1);

        await _context.Database
            .RunCommandAsync<BsonDocument>(
                command,
                cancellationToken: cancellationToken);

        return true;
    }
}