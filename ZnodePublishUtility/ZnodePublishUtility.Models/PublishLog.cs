using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZnodePublishUtility.Models;

public enum PublishLogLevel
{
    Info,
    Warning,
    Error,
    Success
}

public class PublishLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("jobId")]
    public string JobId { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("level")]
    public string Level { get; set; } = "info"; // stored as string for flexibility

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("details")]
    public string? Details { get; set; }
}
