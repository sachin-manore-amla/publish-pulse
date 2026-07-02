namespace ZnodePublishUtility.Models.DTOs;

public class PublishLogDto
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class PublishProgressDto
{
    public string Stage { get; set; } = string.Empty;
    public int Percent { get; set; }
    public string Message { get; set; } = string.Empty;
    public PublishProgressDetailsDto? Details { get; set; }
}

public class PublishProgressDetailsDto
{
    public int? TotalProducts { get; set; }
    public int? IndexedProducts { get; set; }
    public int? RemainingProducts { get; set; }
    public string? EstimatedTime { get; set; }
    public string? ActualTime { get; set; }
}

public class PublishErrorDto
{
    public string Message { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class PublishJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public PublishProgressDto? Progress { get; set; }
    public PublishErrorDto? Error { get; set; }
    public List<PublishLogDto> Logs { get; set; } = new();
}

public class StartPublishDto
{
    public string Type { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}
