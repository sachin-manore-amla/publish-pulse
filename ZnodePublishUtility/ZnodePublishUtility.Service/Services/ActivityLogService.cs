using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repo;

    public ActivityLogService(IActivityLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<ActivityLogDto> CreateAsync(CreateActivityLogDto dto, string? ipAddress = null, string? userAgent = null)
    {
        var log = new ActivityLog
        {
            Level = dto.Level ?? "info",
            Source = dto.Source ?? string.Empty,
            Action = dto.Action ?? string.Empty,
            Message = dto.Message ?? string.Empty,
            Details = dto.Details,
            JobId = dto.JobId,
            TargetName = dto.TargetName,
            TargetId = dto.TargetId,
            UserId = dto.UserId,
            Metadata = dto.Metadata,
            DurationMs = dto.DurationMs,
            ErrorMessage = dto.ErrorMessage,
            CorrelationId = dto.CorrelationId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };
        var created = await _repo.CreateAsync(log);
        return MapToDto(created);
    }

    public async Task<(List<ActivityLogDto> Items, int Total)> GetPagedAsync(ActivityLogFilterDto filter)
    {
        var items = await _repo.GetAllAsync(filter);
        var total = await _repo.GetCountAsync(filter);
        return (items.Select(MapToDto).ToList(), total);
    }

    public async Task<ActivityLogDto?> GetByIdAsync(string id)
    {
        var log = await _repo.GetByIdAsync(id);
        return log == null ? null : MapToDto(log);
    }

    public Task<ActivityLogStatsDto> GetStatsAsync() => _repo.GetStatsAsync();

    public async Task<List<ActivityLogDto>> GetRecentAsync(int limit = 100)
    {
        var logs = await _repo.GetRecentAsync(limit);
        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<ActivityLogDto>> GetByJobIdAsync(string jobId)
    {
        var logs = await _repo.GetByJobIdAsync(jobId);
        return logs.Select(MapToDto).ToList();
    }

    // ── Publish logs ─────────────────────────────────────────────────────────

    public async Task<PublishLogDto> CreatePublishLogAsync(CreatePublishLogDto dto)
    {
        var log = new PublishLog
        {
            JobId = dto.JobId,
            Level = dto.Level ?? "info",
            Message = dto.Message,
            Details = dto.Details,
        };
        var created = await _repo.CreatePublishLogAsync(log);
        return MapPublishLogToDto(created);
    }

    public async Task BulkCreatePublishLogsAsync(BulkCreatePublishLogDto dto)
    {
        var logs = dto.Logs.Select(l => new PublishLog
        {
            JobId = dto.JobId,
            Level = l.Level ?? "info",
            Message = l.Message,
            Details = l.Details,
        }).ToList();
        await _repo.BulkCreatePublishLogsAsync(logs);
    }

    public async Task<List<PublishLogDto>> GetPublishLogsByJobIdAsync(string jobId)
    {
        var logs = await _repo.GetPublishLogsByJobIdAsync(jobId);
        return logs.Select(MapPublishLogToDto).ToList();
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static ActivityLogDto MapToDto(ActivityLog l) => new()
    {
        Id = l.Id,
        Timestamp = l.Timestamp,
        Level = l.Level,
        Source = l.Source,
        Action = l.Action,
        Message = l.Message,
        Details = l.Details,
        JobId = l.JobId,
        TargetName = l.TargetName,
        TargetId = l.TargetId,
        UserId = l.UserId,
        Metadata = l.Metadata,
        DurationMs = l.DurationMs,
        ErrorMessage = l.ErrorMessage,
        CorrelationId = l.CorrelationId,
    };

    private static PublishLogDto MapPublishLogToDto(PublishLog l) => new()
    {
        Id = l.Id,
        JobId = l.JobId,
        Timestamp = l.Timestamp,
        Level = l.Level,
        Message = l.Message,
        Details = l.Details,
    };
}
