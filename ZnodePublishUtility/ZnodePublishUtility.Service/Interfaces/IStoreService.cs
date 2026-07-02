using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface IStoreService
{
    Task<List<StoreDto>> GetAllStoresAsync();
    Task<StoreDto?> GetStoreByIdAsync(string id);
    Task<StoreDto> CreateStoreAsync(CreateStoreDto dto, string userId);
    Task<StoreDto?> UpdateStoreAsync(string id, UpdateStoreDto dto);
    Task<bool> DeleteStoreAsync(string id);
}
