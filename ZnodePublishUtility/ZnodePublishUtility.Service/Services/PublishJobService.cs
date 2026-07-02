using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class PublishJobService : IPublishJobService
{
    private readonly IPublishJobRepository _repository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IPortalRepository _portalRepository;

    public PublishJobService(
        IPublishJobRepository repository,
        ICatalogRepository catalogRepository,
        IStoreRepository storeRepository,
        IPortalRepository portalRepository)
    {
        _repository = repository;
        _catalogRepository = catalogRepository;
        _storeRepository = storeRepository;
        _portalRepository = portalRepository;
    }

    public async Task<List<PublishJobDto>> GetAllJobsAsync()
    {
        var jobs = await _repository.GetAllJobsAsync();
        var dtos = new List<PublishJobDto>();

        foreach (var job in jobs)
        {
            var logs = await _repository.GetJobLogsAsync(job.Id);
            var progress = await _repository.GetProgressAsync(job.Id);
            dtos.Add(MapToDto(job, logs, progress));
        }

        return dtos;
    }

    public async Task<PublishJobDto?> GetJobByIdAsync(string id)
    {
        var job = await _repository.GetJobByIdAsync(id);
        if (job == null)
            return null;

        var logs = await _repository.GetJobLogsAsync(id);
        var progress = await _repository.GetProgressAsync(id);

        return MapToDto(job, logs, progress);
    }

    public async Task<PublishJobDto> StartPublishAsync(StartPublishDto dto, string userId)
    {
        // Validate publish type
        if (!Enum.TryParse<PublishType>(dto.Type, true, out var publishType))
            throw new InvalidOperationException($"Invalid publish type: {dto.Type}");

        // Validate target exists
        var targetName = await ValidateAndGetTargetNameAsync(publishType, dto.TargetId);

        var job = new PublishJob
        {
            Type = publishType,
            Status = PublishStatus.Queued,
            TargetId = dto.TargetId,
            TargetName = targetName,
            CreatedBy = userId
        };

        var created = await _repository.CreateJobAsync(job);

        // Add initial log
        await _repository.AddLogAsync(new PublishLog
        {
            JobId = created.Id,
            Level = "info",
            Message = $"{publishType} publish job started for {targetName}"
        });

        var logs = await _repository.GetJobLogsAsync(created.Id);
        return MapToDto(created, logs, null);
    }

    public async Task<bool> CancelPublishAsync(string jobId)
    {
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null || job.Status == PublishStatus.Completed || job.Status == PublishStatus.Failed)
            return false;

        job.Status = PublishStatus.Failed;
        job.EndTime = DateTime.UtcNow;
        job.Error = new PublishError
        {
            Message = "Publish job cancelled by user",
            Stage = "Cancellation",
            CorrelationId = $"CANCEL-{jobId}"
        };

        await _repository.UpdateJobAsync(jobId, job);
        return true;
    }

    public async Task<List<PublishLogDto>> GetJobLogsAsync(string jobId)
    {
        var logs = await _repository.GetJobLogsAsync(jobId);
        return logs.Select(MapLogToDto).ToList();
    }

    public async Task<PublishProgressDto?> GetJobProgressAsync(string jobId)
    {
        var progress = await _repository.GetProgressAsync(jobId);
        if (progress == null)
            return null;

        return MapProgressToDto(progress);
    }

    public async Task UpdateJobStatusAsync(string jobId, PublishStatus status)
    {
        var job = await _repository.GetJobByIdAsync(jobId);
        if (job == null)
            throw new InvalidOperationException($"Job {jobId} not found");

        job.Status = status;
        if (status == PublishStatus.Completed || status == PublishStatus.Failed)
        {
            job.EndTime = DateTime.UtcNow;
        }

        await _repository.UpdateJobAsync(jobId, job);
    }

    private async Task<string> ValidateAndGetTargetNameAsync(PublishType type, string targetId)
    {
        return type switch
        {
            PublishType.Catalog => (await _catalogRepository.GetCatalogByIdAsync(targetId))?.Name 
                ?? throw new InvalidOperationException($"Catalog {targetId} not found"),
            PublishType.Store => (await _storeRepository.GetStoreByIdAsync(targetId))?.Name 
                ?? throw new InvalidOperationException($"Store {targetId} not found"),
            PublishType.CMS => (await _portalRepository.GetPortalByIdAsync(targetId))?.Name 
                ?? throw new InvalidOperationException($"Portal {targetId} not found"),
            _ => throw new InvalidOperationException($"Unknown publish type: {type}")
        };
    }

    private static PublishJobDto MapToDto(PublishJob job, List<PublishLog> logs, PublishProgress? progress)
    {
        return new PublishJobDto
        {
            Id = job.Id,
            Type = job.Type.ToString(),
            Status = job.Status.ToString(),
            TargetId = job.TargetId,
            TargetName = job.TargetName,
            StartTime = job.StartTime,
            EndTime = job.EndTime,
            Progress = progress != null ? MapProgressToDto(progress) : null,
            Error = job.Error != null ? MapErrorToDto(job.Error) : null,
            Logs = logs.Select(MapLogToDto).ToList()
        };
    }

    private static PublishProgressDto MapProgressToDto(PublishProgress progress)
    {
        return new PublishProgressDto
        {
            Stage = progress.Stage,
            Percent = progress.Percent,
            Message = progress.Message,
            Details = new PublishProgressDetailsDto
            {
                TotalProducts = progress.TotalProducts,
                IndexedProducts = progress.IndexedProducts,
                RemainingProducts = progress.RemainingProducts,
                EstimatedTime = progress.EstimatedTime,
                ActualTime = progress.ActualTime
            }
        };
    }

    private static PublishErrorDto MapErrorToDto(PublishError error)
    {
        return new PublishErrorDto
        {
            Message = error.Message,
            Stage = error.Stage,
            CorrelationId = error.CorrelationId
        };
    }

    private static PublishLogDto MapLogToDto(PublishLog log)
    {
        return new PublishLogDto
        {
            Id = log.Id,
            JobId = log.JobId,
            Timestamp = log.Timestamp,
            Level = log.Level.ToString(),
            Message = log.Message,
            Details = log.Details
        };
    }
}
