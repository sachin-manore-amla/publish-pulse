using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Interfaces;

public interface IPublishJobRepository
{
    Task<List<PublishJob>> GetAllJobsAsync();
    Task<List<PublishJob>> GetJobsByStatusAsync(PublishStatus status);
    Task<PublishJob?> GetJobByIdAsync(string id);
    Task<PublishJob> CreateJobAsync(PublishJob job);
    Task<PublishJob?> UpdateJobAsync(string id, PublishJob job);
    Task<bool> DeleteJobAsync(string id);
    Task AddLogAsync(PublishLog log);
    Task<List<PublishLog>> GetJobLogsAsync(string jobId);
    Task UpdateProgressAsync(string jobId, PublishProgress progress);
    Task<PublishProgress?> GetProgressAsync(string jobId);
}
