using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.MongoDB;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    static MongoDbContext()
    {
        // Ignore any extra elements in documents (forward-compatibility)
        ConventionRegistry.Register(
            "IgnoreExtraElements",
            new ConventionPack { new IgnoreExtraElementsConvention(true) },
            _ => true);

        // Enum stored as string (not integer)
        ConventionRegistry.Register(
            "EnumAsString",
            new ConventionPack { new EnumRepresentationConvention(BsonType.String) },
            _ => true);
    }

    public MongoDbContext(string connectionString, string databaseName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);
        _database = client.GetDatabase(databaseName);
        EnsureIndexes();
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
        => _database.GetCollection<T>(collectionName);

    public IMongoCollection<PublishJob> PublishJobs
        => GetCollection<PublishJob>("publish_jobs");

    public IMongoCollection<PublishHistory> PublishHistories
        => GetCollection<PublishHistory>("publish_history");

    public IMongoCollection<PublishLog> PublishLogs
        => GetCollection<PublishLog>("publish_logs");

    public IMongoCollection<ActivityLog> ActivityLogs
        => GetCollection<ActivityLog>("activity_logs");

    private void EnsureIndexes()
    {
        // publish_jobs indexes
        PublishJobs.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<PublishJob>(
                Builders<PublishJob>.IndexKeys.Ascending(j => j.Status),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<PublishJob>(
                Builders<PublishJob>.IndexKeys.Descending(j => j.CreatedAt),
                new CreateIndexOptions { Background = true }),
        });

        // publish_logs indexes
        PublishLogs.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<PublishLog>(
                Builders<PublishLog>.IndexKeys.Ascending(l => l.JobId),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<PublishLog>(
                Builders<PublishLog>.IndexKeys.Descending(l => l.Timestamp),
                new CreateIndexOptions { Background = true }),
        });

        // publish_history indexes
        PublishHistories.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<PublishHistory>(
                Builders<PublishHistory>.IndexKeys.Descending(h => h.CreatedAt),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<PublishHistory>(
                Builders<PublishHistory>.IndexKeys.Ascending(h => h.Status),
                new CreateIndexOptions { Background = true }),
        });

        // activity_logs indexes (with TTL: auto-expire after 90 days)
        ActivityLogs.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ActivityLog>(
                Builders<ActivityLog>.IndexKeys.Descending(a => a.Timestamp),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<ActivityLog>(
                Builders<ActivityLog>.IndexKeys.Ascending(a => a.Level),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<ActivityLog>(
                Builders<ActivityLog>.IndexKeys.Ascending(a => a.Source),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<ActivityLog>(
                Builders<ActivityLog>.IndexKeys.Ascending(a => a.JobId),
                new CreateIndexOptions { Background = true }),
            new CreateIndexModel<ActivityLog>(
                Builders<ActivityLog>.IndexKeys.Ascending(a => a.Timestamp),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90), Background = true }),
        });
    }
}
