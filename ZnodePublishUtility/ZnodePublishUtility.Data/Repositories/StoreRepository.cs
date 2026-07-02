using ZnodePublishUtility.Models;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MockData;

namespace ZnodePublishUtility.Data.Repositories;

public class StoreRepository : IStoreRepository
{
    private static readonly List<Store> _stores = MockDataProvider.GetMockStores();

    public Task<List<Store>> GetAllStoresAsync()
    {
        return Task.FromResult(_stores.Where(s => s.IsActive).ToList());
    }

    public Task<Store?> GetStoreByIdAsync(string id)
    {
        var store = _stores.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(store);
    }

    public Task<Store> CreateStoreAsync(Store store)
    {
        store.Id = Guid.NewGuid().ToString().Substring(0, 8);
        store.CreatedAt = DateTime.UtcNow;
        store.IsActive = true;
        _stores.Add(store);
        return Task.FromResult(store);
    }

    public Task<Store?> UpdateStoreAsync(string id, Store store)
    {
        var existingStore = _stores.FirstOrDefault(s => s.Id == id);
        if (existingStore == null)
            return Task.FromResult<Store?>(null);

        existingStore.Name = store.Name;
        existingStore.Domain = store.Domain;
        existingStore.IsActive = store.IsActive;
        existingStore.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Store?>(existingStore);
    }

    public Task<bool> DeleteStoreAsync(string id)
    {
        var store = _stores.FirstOrDefault(s => s.Id == id);
        if (store == null)
            return Task.FromResult(false);

        _stores.Remove(store);
        return Task.FromResult(true);
    }

    public Task<bool> StoreExistsAsync(string id)
    {
        return Task.FromResult(_stores.Any(s => s.Id == id));
    }
}
