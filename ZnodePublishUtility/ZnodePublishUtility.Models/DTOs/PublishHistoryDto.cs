namespace ZnodePublishUtility.Models.DTOs;

public class PublishHistoryDetailDto
{
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int? FailedRecords { get; set; }
}

public class PublishHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Duration { get; set; } = string.Empty;
    public PublishErrorDto? Error { get; set; }
    public PublishHistoryDetailDto? Details { get; set; }
    public List<PublishLogDto> Logs { get; set; } = new();
}
