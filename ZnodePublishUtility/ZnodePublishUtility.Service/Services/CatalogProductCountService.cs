using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.Service.Services;

public class CatalogProductCountService : ICatalogProductCountService
{
    private readonly ICatalogProductCountRepository _repository;

    public CatalogProductCountService(ICatalogProductCountRepository repository)
    {
        _repository = repository;
    }

    public Task<List<CatalogProductCountDto>> GetCatalogProductCountsAsync() =>
        _repository.GetCatalogProductCountsAsync();
}
