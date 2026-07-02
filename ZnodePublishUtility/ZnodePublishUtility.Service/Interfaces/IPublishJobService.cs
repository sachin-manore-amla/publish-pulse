using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface IPublishJobService
{
    Task<List<PublishJobDto>> GetAllJobsAsync();
    Task<PublishJobDto?> GetJobByIdAsync(string id);
    Task<PublishJobDto> StartPublishAsync(StartPublishDto dto, string userId);
    Task<bool> CancelPublishAsync(string jobId);
    Task<List<PublishLogDto>> GetJobLogsAsync(string jobId);
    Task<PublishProgressDto?> GetJobProgressAsync(string jobId);
    Task UpdateJobStatusAsync(string jobId, PublishStatus status);
}
