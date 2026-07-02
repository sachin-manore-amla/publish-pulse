using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Interfaces;

public interface IStoreRepository
{
    Task<List<Store>> GetAllStoresAsync();
    Task<Store?> GetStoreByIdAsync(string id);
    Task<Store> CreateStoreAsync(Store store);
    Task<Store?> UpdateStoreAsync(string id, Store store);
    Task<bool> DeleteStoreAsync(string id);
    Task<bool> StoreExistsAsync(string id);
}
