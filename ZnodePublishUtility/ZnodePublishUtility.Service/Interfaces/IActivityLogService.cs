using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface IActivityLogService
{
    Task<ActivityLogDto> CreateAsync(CreateActivityLogDto dto, string? ipAddress = null, string? userAgent = null);
    Task<(List<ActivityLogDto> Items, int Total)> GetPagedAsync(ActivityLogFilterDto filter);
    Task<ActivityLogDto?> GetByIdAsync(string id);
    Task<ActivityLogStatsDto> GetStatsAsync();
    Task<List<ActivityLogDto>> GetRecentAsync(int limit = 100);
    Task<List<ActivityLogDto>> GetByJobIdAsync(string jobId);

    // Publish log management
    Task<PublishLogDto> CreatePublishLogAsync(CreatePublishLogDto dto);
    Task BulkCreatePublishLogsAsync(BulkCreatePublishLogDto dto);
    Task<List<PublishLogDto>> GetPublishLogsByJobIdAsync(string jobId);
}
