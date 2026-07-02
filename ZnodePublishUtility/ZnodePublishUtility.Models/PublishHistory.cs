using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ZnodePublishUtility.Models;

public class PublishHistoryDetail
{
    [BsonElement("totalRecords")]
    public int TotalRecords { get; set; }

    [BsonElement("processedRecords")]
    public int ProcessedRecords { get; set; }

    [BsonElement("failedRecords")]
    public int? FailedRecords { get; set; }
}

public class PublishHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public PublishType Type { get; set; }

    [BsonElement("targetName")]
    public string TargetName { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public PublishStatus Status { get; set; }

    [BsonElement("startTime")]
    public DateTime StartTime { get; set; }

    [BsonElement("endTime")]
    public DateTime EndTime { get; set; }

    [BsonElement("duration")]
    public string Duration { get; set; } = string.Empty;

    [BsonElement("error")]
    public PublishError? Error { get; set; }

    [BsonElement("details")]
    public PublishHistoryDetail? Details { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
