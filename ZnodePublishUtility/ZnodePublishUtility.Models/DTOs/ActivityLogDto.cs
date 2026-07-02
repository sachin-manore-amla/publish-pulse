namespace ZnodePublishUtility.Models.DTOs;

public class ActivityLogDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? JobId { get; set; }
    public string? TargetName { get; set; }
    public string? TargetId { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}

public class CreateActivityLogDto
{
    public string Level { get; set; } = "info";
    public string Source { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? JobId { get; set; }
    public string? TargetName { get; set; }
    public string? TargetId { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}

public class ActivityLogFilterDto
{
    public string? Level { get; set; }
    public string? Source { get; set; }
    public string? Action { get; set; }
    public string? JobId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ActivityLogStatsDto
{
    public int TotalLogs { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int SuccessCount { get; set; }
    public int PublishStartedToday { get; set; }
    public int PublishCompletedToday { get; set; }
    public int PublishFailedToday { get; set; }
    public Dictionary<string, int> BySource { get; set; } = new();
    public DateTime? LastActivity { get; set; }
}

public class CreatePublishLogDto
{
    public string JobId { get; set; } = string.Empty;
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class BulkCreatePublishLogDto
{
    public string JobId { get; set; } = string.Empty;
    public List<CreatePublishLogDto> Logs { get; set; } = new();
}
