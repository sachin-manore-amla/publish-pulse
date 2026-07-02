using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Data.Interfaces;

public interface IActivityLogRepository
{
    Task<ActivityLog> CreateAsync(ActivityLog log);
    Task<List<ActivityLog>> GetAllAsync(ActivityLogFilterDto filter);
    Task<int> GetCountAsync(ActivityLogFilterDto filter);
    Task<ActivityLog?> GetByIdAsync(string id);
    Task<ActivityLogStatsDto> GetStatsAsync();
    Task<List<ActivityLog>> GetRecentAsync(int limit = 100);
    Task<List<ActivityLog>> GetByJobIdAsync(string jobId);
    Task DeleteOlderThanAsync(DateTime cutoff);

    // Publish log specific
    Task<PublishLog> CreatePublishLogAsync(PublishLog log);
    Task<List<PublishLog>> GetPublishLogsByJobIdAsync(string jobId);
    Task BulkCreatePublishLogsAsync(List<PublishLog> logs);
}
