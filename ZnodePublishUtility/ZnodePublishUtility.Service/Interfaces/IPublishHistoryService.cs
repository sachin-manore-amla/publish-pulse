using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface IPublishHistoryService
{
    Task<List<PublishHistoryDto>> GetAllHistoryAsync();
    Task<List<PublishHistoryDto>> GetHistoryByTypeAsync(string type);
    Task<List<PublishHistoryDto>> GetHistoryByStatusAsync(string status);
    Task<PublishHistoryDto?> GetHistoryByIdAsync(string id);
    Task<List<PublishHistoryDto>> GetHistoryByTargetNameAsync(string targetName);
}
