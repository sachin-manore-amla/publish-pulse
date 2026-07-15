using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Data.Interfaces;

/// <summary>
/// Reads catalog product counts from SQL Server via <c>dbo.GetCatalogProductCounts</c>.
/// </summary>
public interface ICatalogProductCountRepository
{
    /// <summary>
    /// Executes <c>EXEC dbo.GetCatalogProductCounts;</c> — a two-column result set (catalog id,
    /// then draft product count).
    /// </summary>
    Task<List<CatalogProductCountDto>> GetCatalogProductCountsAsync();
}
