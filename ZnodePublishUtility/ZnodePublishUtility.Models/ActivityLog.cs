using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZnodePublishUtility.Models;

public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("level")]
    public string Level { get; set; } = "info"; // info | warning | error | success

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty; // catalog | store | cms | system | api | health

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // publish_started | publish_completed | publish_failed | publish_cancelled | health_check | config_viewed

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("details")]
    public string? Details { get; set; }

    [BsonElement("jobId")]
    public string? JobId { get; set; }

    [BsonElement("targetName")]
    public string? TargetName { get; set; }

    [BsonElement("targetId")]
    public string? TargetId { get; set; }

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("durationMs")]
    public long? DurationMs { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("correlationId")]
    public string? CorrelationId { get; set; }
}
