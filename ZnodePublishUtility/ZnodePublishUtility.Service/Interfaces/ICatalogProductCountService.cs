using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Service.Interfaces;

public interface ICatalogProductCountService
{
    Task<List<CatalogProductCountDto>> GetCatalogProductCountsAsync();
}
