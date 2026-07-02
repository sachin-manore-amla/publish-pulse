using ZnodePublishUtility.Models;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MockData;

namespace ZnodePublishUtility.Data.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private static readonly List<Catalog> _catalogs = MockDataProvider.GetMockCatalogs();

    public Task<List<Catalog>> GetAllCatalogsAsync()
    {
        return Task.FromResult(_catalogs.Where(c => c.IsActive).ToList());
    }

    public Task<Catalog?> GetCatalogByIdAsync(string id)
    {
        var catalog = _catalogs.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(catalog);
    }

    public Task<Catalog> CreateCatalogAsync(Catalog catalog)
    {
        catalog.Id = Guid.NewGuid().ToString().Substring(0, 8);
        catalog.CreatedAt = DateTime.UtcNow;
        catalog.IsActive = true;
        _catalogs.Add(catalog);
        return Task.FromResult(catalog);
    }

    public Task<Catalog?> UpdateCatalogAsync(string id, Catalog catalog)
    {
        var existingCatalog = _catalogs.FirstOrDefault(c => c.Id == id);
        if (existingCatalog == null)
            return Task.FromResult<Catalog?>(null);

        existingCatalog.Name = catalog.Name;
        existingCatalog.ProductCount = catalog.ProductCount;
        existingCatalog.IsActive = catalog.IsActive;
        existingCatalog.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<Catalog?>(existingCatalog);
    }

    public Task<bool> DeleteCatalogAsync(string id)
    {
        var catalog = _catalogs.FirstOrDefault(c => c.Id == id);
        if (catalog == null)
            return Task.FromResult(false);

        _catalogs.Remove(catalog);
        return Task.FromResult(true);
    }

    public Task<bool> CatalogExistsAsync(string id)
    {
        return Task.FromResult(_catalogs.Any(c => c.Id == id));
    }
}
