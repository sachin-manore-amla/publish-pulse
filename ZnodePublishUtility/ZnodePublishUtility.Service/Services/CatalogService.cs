using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _repository;

    public CatalogService(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CatalogDto>> GetAllCatalogsAsync()
    {
        var catalogs = await _repository.GetAllCatalogsAsync();
        return catalogs.Select(MapToDto).ToList();
    }

    public async Task<CatalogDto?> GetCatalogByIdAsync(string id)
    {
        var catalog = await _repository.GetCatalogByIdAsync(id);
        return catalog == null ? null : MapToDto(catalog);
    }

    public async Task<CatalogDto> CreateCatalogAsync(CreateCatalogDto dto, string userId)
    {
        var catalog = new Catalog
        {
            Name = dto.Name,
            ProductCount = dto.ProductCount,
            CreatedBy = userId
        };

        var created = await _repository.CreateCatalogAsync(catalog);
        return MapToDto(created);
    }

    public async Task<CatalogDto?> UpdateCatalogAsync(string id, UpdateCatalogDto dto)
    {
        var catalog = new Catalog
        {
            Name = dto.Name,
            ProductCount = dto.ProductCount,
            IsActive = dto.IsActive
        };

        var updated = await _repository.UpdateCatalogAsync(id, catalog);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteCatalogAsync(string id)
    {
        return await _repository.DeleteCatalogAsync(id);
    }

    private static CatalogDto MapToDto(Catalog catalog)
    {
        return new CatalogDto
        {
            Id = catalog.Id,
            Name = catalog.Name,
            ProductCount = catalog.ProductCount,
            LastPublished = catalog.LastPublished,
            CreatedAt = catalog.CreatedAt,
            UpdatedAt = catalog.UpdatedAt,
            CreatedBy = catalog.CreatedBy,
            IsActive = catalog.IsActive
        };
    }
}
