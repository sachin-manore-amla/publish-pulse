using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class PortalService : IPortalService
{
    private readonly IPortalRepository _repository;
    private readonly IStoreRepository _storeRepository;

    public PortalService(IPortalRepository repository, IStoreRepository storeRepository)
    {
        _repository = repository;
        _storeRepository = storeRepository;
    }

    public async Task<List<PortalDto>> GetAllPortalsAsync()
    {
        var portals = await _repository.GetAllPortalsAsync();
        return portals.Select(MapToDto).ToList();
    }

    public async Task<List<PortalDto>> GetPortalsByStoreIdAsync(string storeId)
    {
        var portals = await _repository.GetPortalsByStoreIdAsync(storeId);
        return portals.Select(MapToDto).ToList();
    }

    public async Task<PortalDto?> GetPortalByIdAsync(string id)
    {
        var portal = await _repository.GetPortalByIdAsync(id);
        return portal == null ? null : MapToDto(portal);
    }

    public async Task<PortalDto> CreatePortalAsync(CreatePortalDto dto, string userId)
    {
        var store = await _storeRepository.GetStoreByIdAsync(dto.StoreId);
        if (store == null)
            throw new InvalidOperationException($"Store with ID {dto.StoreId} not found");

        var portal = new Portal
        {
            Name = dto.Name,
            StoreId = dto.StoreId,
            StoreName = store.Name,
            CreatedBy = userId
        };

        var created = await _repository.CreatePortalAsync(portal);
        return MapToDto(created);
    }

    public async Task<PortalDto?> UpdatePortalAsync(string id, UpdatePortalDto dto)
    {
        var portal = new Portal
        {
            Name = dto.Name,
            IsActive = dto.IsActive
        };

        var updated = await _repository.UpdatePortalAsync(id, portal);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeletePortalAsync(string id)
    {
        return await _repository.DeletePortalAsync(id);
    }

    private static PortalDto MapToDto(Portal portal)
    {
        return new PortalDto
        {
            Id = portal.Id,
            Name = portal.Name,
            StoreId = portal.StoreId,
            StoreName = portal.StoreName,
            LastPublished = portal.LastPublished,
            CreatedAt = portal.CreatedAt,
            UpdatedAt = portal.UpdatedAt,
            CreatedBy = portal.CreatedBy,
            IsActive = portal.IsActive
        };
    }
}
