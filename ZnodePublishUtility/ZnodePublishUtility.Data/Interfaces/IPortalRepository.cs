using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Interfaces;

public interface IPortalRepository
{
    Task<List<Portal>> GetAllPortalsAsync();
    Task<List<Portal>> GetPortalsByStoreIdAsync(string storeId);
    Task<Portal?> GetPortalByIdAsync(string id);
    Task<Portal> CreatePortalAsync(Portal portal);
    Task<Portal?> UpdatePortalAsync(string id, Portal portal);
    Task<bool> DeletePortalAsync(string id);
    Task<bool> PortalExistsAsync(string id);
}
