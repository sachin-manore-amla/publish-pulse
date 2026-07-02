using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface ICatalogService
{
    Task<List<CatalogDto>> GetAllCatalogsAsync();
    Task<CatalogDto?> GetCatalogByIdAsync(string id);
    Task<CatalogDto> CreateCatalogAsync(CreateCatalogDto dto, string userId);
    Task<CatalogDto?> UpdateCatalogAsync(string id, UpdateCatalogDto dto);
    Task<bool> DeleteCatalogAsync(string id);
}
