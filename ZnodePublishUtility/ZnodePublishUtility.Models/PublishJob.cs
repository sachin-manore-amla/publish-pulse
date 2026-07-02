using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZnodePublishUtility.Models;

public enum PublishType
{
    Catalog,
    Store,
    CMS
}

public enum PublishStatus
{
    Idle,
    Queued,
    InProgress,
    Completed,
    Failed
}

public class PublishError
{
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("stage")]
    public string Stage { get; set; } = string.Empty;

    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;
}

public class PublishJob
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public PublishType Type { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public PublishStatus Status { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("targetName")]
    public string TargetName { get; set; } = string.Empty;

    [BsonElement("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [BsonElement("endTime")]
    public DateTime? EndTime { get; set; }

    [BsonElement("progress")]
    public PublishProgress? Progress { get; set; }

    [BsonElement("error")]
    public PublishError? Error { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;
}
