namespace AeroResponse.Data.Mongo;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    /// <summary>
    /// Controls whether the application attempts to use MongoDB.
    ///
    /// When false, account creation continues without trying to connect.
    /// </summary>
    public bool Enabled { get; init; }

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = "AeroResponse";

    public string AccountsCollectionName { get; init; } = "accounts";

    /// <summary>
    /// Maximum time spent selecting an available MongoDB server.
    /// </summary>
    public int ServerSelectionTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Maximum time spent establishing a network connection.
    /// </summary>
    public int ConnectTimeoutSeconds { get; init; } = 5;

    /// <summary>
    /// Maximum duration of one account-writing operation.
    /// </summary>
    public int OperationTimeoutSeconds { get; init; } = 8;
}