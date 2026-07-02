using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Interfaces;

public interface IPublishHistoryRepository
{
    Task<List<PublishHistory>> GetAllHistoryAsync();
    Task<List<PublishHistory>> GetHistoryByTypeAsync(PublishType type);
    Task<List<PublishHistory>> GetHistoryByStatusAsync(PublishStatus status);
    Task<PublishHistory?> GetHistoryByIdAsync(string id);
    Task<PublishHistory> CreateHistoryAsync(PublishHistory history);
    Task<List<PublishHistory>> GetHistoryByTargetNameAsync(string targetName);
}
