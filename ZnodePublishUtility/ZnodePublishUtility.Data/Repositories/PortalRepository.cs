using ZnodePublishUtility.Models;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MockData;

namespace ZnodePublishUtility.Data.Repositories;

public class PortalRepository : IPortalRepository
{
    private static readonly List<Portal> _portals = MockDataProvider.GetMockPortals();

    public Task<List<Portal>> GetAllPortalsAsync()
    {
        return Task.FromResult(_portals.Where(p => p.IsActive).ToList());
    }

    public Task<List<Portal>> GetPortalsByStoreIdAsync(string storeId)
    {
        return Task.FromResult(_portals.Where(p => p.StoreId == storeId && p.IsActive).ToList());
    }

    public Task<Portal?> GetPortalByIdAsync(string id)
    {
        var portal = _portals.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(portal);
    }

    public Task<Portal> CreatePortalAsync(Portal portal)
    {
        portal.Id = Guid.NewGuid().ToString().Substring(0, 8);
        portal.CreatedAt = DateTime.UtcNow;
        portal.IsActive = true;
        _portals.Add(portal);
        return Task.FromResult(portal);
    }

    public Task<Portal?> UpdatePortalAsync(string id, Portal portal)
    {
        var existingPortal = _portals.FirstOrDefault(p => p.Id == id);
        if (existingPortal == null)
            return Task.FromResult<Portal?>(null);

        existingPortal.Name = portal.Name;
        existingPortal.IsActive = portal.IsActive;
        existingPortal.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Portal?>(existingPortal);
    }

    public Task<bool> DeletePortalAsync(string id)
    {
        var portal = _portals.FirstOrDefault(p => p.Id == id);
        if (portal == null)
            return Task.FromResult(false);

        _portals.Remove(portal);
        return Task.FromResult(true);
    }

    public Task<bool> PortalExistsAsync(string id)
    {
        return Task.FromResult(_portals.Any(p => p.Id == id));
    }
}
