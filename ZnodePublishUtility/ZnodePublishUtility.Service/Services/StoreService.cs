using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _repository;

    public StoreService(IStoreRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<StoreDto>> GetAllStoresAsync()
    {
        var stores = await _repository.GetAllStoresAsync();
        return stores.Select(MapToDto).ToList();
    }

    public async Task<StoreDto?> GetStoreByIdAsync(string id)
    {
        var store = await _repository.GetStoreByIdAsync(id);
        return store == null ? null : MapToDto(store);
    }

    public async Task<StoreDto> CreateStoreAsync(CreateStoreDto dto, string userId)
    {
        var store = new Store
        {
            Name = dto.Name,
            Domain = dto.Domain,
            CreatedBy = userId
        };

        var created = await _repository.CreateStoreAsync(store);
        return MapToDto(created);
    }

    public async Task<StoreDto?> UpdateStoreAsync(string id, UpdateStoreDto dto)
    {
        var store = new Store
        {
            Name = dto.Name,
            Domain = dto.Domain,
            IsActive = dto.IsActive
        };

        var updated = await _repository.UpdateStoreAsync(id, store);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteStoreAsync(string id)
    {
        return await _repository.DeleteStoreAsync(id);
    }

    private static StoreDto MapToDto(Store store)
    {
        return new StoreDto
        {
            Id = store.Id,
            Name = store.Name,
            Domain = store.Domain,
            LastPublished = store.LastPublished,
            CreatedAt = store.CreatedAt,
            UpdatedAt = store.UpdatedAt,
            CreatedBy = store.CreatedBy,
            IsActive = store.IsActive
        };
    }
}
