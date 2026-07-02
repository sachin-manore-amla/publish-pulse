using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Interfaces;

public interface ICatalogRepository
{
    Task<List<Catalog>> GetAllCatalogsAsync();
    Task<Catalog?> GetCatalogByIdAsync(string id);
    Task<Catalog> CreateCatalogAsync(Catalog catalog);
    Task<Catalog?> UpdateCatalogAsync(string id, Catalog catalog);
    Task<bool> DeleteCatalogAsync(string id);
    Task<bool> CatalogExistsAsync(string id);
}
