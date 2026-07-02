using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class PublishHistoryService : IPublishHistoryService
{
    private readonly IPublishHistoryRepository _repository;

    public PublishHistoryService(IPublishHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PublishHistoryDto>> GetAllHistoryAsync()
    {
        var history = await _repository.GetAllHistoryAsync();
        return history.Select(MapToDto).ToList();
    }

    public async Task<List<PublishHistoryDto>> GetHistoryByTypeAsync(string type)
    {
        if (!Enum.TryParse<PublishType>(type, true, out var publishType))
            throw new InvalidOperationException($"Invalid publish type: {type}");

        var history = await _repository.GetHistoryByTypeAsync(publishType);
        return history.Select(MapToDto).ToList();
    }

    public async Task<List<PublishHistoryDto>> GetHistoryByStatusAsync(string status)
    {
        if (!Enum.TryParse<PublishStatus>(status, true, out var publishStatus))
            throw new InvalidOperationException($"Invalid publish status: {status}");

        var history = await _repository.GetHistoryByStatusAsync(publishStatus);
        return history.Select(MapToDto).ToList();
    }

    public async Task<PublishHistoryDto?> GetHistoryByIdAsync(string id)
    {
        var history = await _repository.GetHistoryByIdAsync(id);
        return history == null ? null : MapToDto(history);
    }

    public async Task<List<PublishHistoryDto>> GetHistoryByTargetNameAsync(string targetName)
    {
        var history = await _repository.GetHistoryByTargetNameAsync(targetName);
        return history.Select(MapToDto).ToList();
    }

    private static PublishHistoryDto MapToDto(PublishHistory history)
    {
        return new PublishHistoryDto
        {
            Id = history.Id,
            Type = history.Type.ToString(),
            TargetName = history.TargetName,
            Status = history.Status.ToString(),
            StartTime = history.StartTime,
            EndTime = history.EndTime,
            Duration = history.Duration,
            Error = history.Error != null ? MapErrorToDto(history.Error) : null,
            Details = history.Details != null ? MapDetailsToDto(history.Details) : null,
            Logs = new List<PublishLogDto>() // Logs would be loaded separately if needed
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

    private static PublishHistoryDetailDto MapDetailsToDto(PublishHistoryDetail details)
    {
        return new PublishHistoryDetailDto
        {
            TotalRecords = details.TotalRecords,
            ProcessedRecords = details.ProcessedRecords,
            FailedRecords = details.FailedRecords
        };
    }
}
