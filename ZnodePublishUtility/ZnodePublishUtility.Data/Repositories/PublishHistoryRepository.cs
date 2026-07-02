using MongoDB.Driver;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MongoDB;
using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Repositories;

public class PublishHistoryRepository : IPublishHistoryRepository
{
    private readonly MongoDbContext _db;

    public PublishHistoryRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<PublishHistory>> GetAllHistoryAsync()
    {
        return await _db.PublishHistories
            .Find(_ => true)
            .SortByDescending(h => h.CreatedAt)
            .Limit(500)
            .ToListAsync();
    }

    public async Task<List<PublishHistory>> GetHistoryByTypeAsync(PublishType type)
    {
        return await _db.PublishHistories
            .Find(h => h.Type == type)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PublishHistory>> GetHistoryByStatusAsync(PublishStatus status)
    {
        return await _db.PublishHistories
            .Find(h => h.Status == status)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<PublishHistory?> GetHistoryByIdAsync(string id)
    {
        return await _db.PublishHistories.Find(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<PublishHistory> CreateHistoryAsync(PublishHistory history)
    {
        history.CreatedAt = DateTime.UtcNow;
        await _db.PublishHistories.InsertOneAsync(history);
        return history;
    }

    public async Task<List<PublishHistory>> GetHistoryByTargetNameAsync(string targetName)
    {
        return await _db.PublishHistories
            .Find(h => h.TargetName == targetName)
            .SortByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
}
