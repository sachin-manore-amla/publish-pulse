using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZnodePublishUtility.Models;

public class PublishProgress
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("jobId")]
    public string JobId { get; set; } = string.Empty;

    [BsonElement("stage")]
    public string Stage { get; set; } = string.Empty;

    [BsonElement("percent")]
    public int Percent { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("totalProducts")]
    public int? TotalProducts { get; set; }

    [BsonElement("indexedProducts")]
    public int? IndexedProducts { get; set; }

    [BsonElement("remainingProducts")]
    public int? RemainingProducts { get; set; }

    [BsonElement("estimatedTime")]
    public string? EstimatedTime { get; set; }

    [BsonElement("actualTime")]
    public string? ActualTime { get; set; }

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
