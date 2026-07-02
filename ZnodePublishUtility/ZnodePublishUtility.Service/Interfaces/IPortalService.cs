using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface IPortalService
{
    Task<List<PortalDto>> GetAllPortalsAsync();
    Task<List<PortalDto>> GetPortalsByStoreIdAsync(string storeId);
    Task<PortalDto?> GetPortalByIdAsync(string id);
    Task<PortalDto> CreatePortalAsync(CreatePortalDto dto, string userId);
    Task<PortalDto?> UpdatePortalAsync(string id, UpdatePortalDto dto);
    Task<bool> DeletePortalAsync(string id);
}
