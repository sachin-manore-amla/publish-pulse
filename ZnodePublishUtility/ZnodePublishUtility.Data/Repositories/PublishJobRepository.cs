using MongoDB.Driver;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MongoDB;
using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.Repositories;

public class PublishJobRepository : IPublishJobRepository
{
    private readonly MongoDbContext _db;

    public PublishJobRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<PublishJob>> GetAllJobsAsync()
    {
        return await _db.PublishJobs
            .Find(_ => true)
            .SortByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PublishJob>> GetJobsByStatusAsync(PublishStatus status)
    {
        return await _db.PublishJobs
            .Find(j => j.Status == status)
            .SortByDescending(j => j.CreatedAt)
            .ToListAsync();
    }

    public async Task<PublishJob?> GetJobByIdAsync(string id)
    {
        return await _db.PublishJobs.Find(j => j.Id == id).FirstOrDefaultAsync();
    }

    public async Task<PublishJob> CreateJobAsync(PublishJob job)
    {
        job.Id = Guid.NewGuid().ToString("N")[..8];
        job.CreatedAt = DateTime.UtcNow;
        job.StartTime = DateTime.UtcNow;
        await _db.PublishJobs.InsertOneAsync(job);
        return job;
    }

    public async Task<PublishJob?> UpdateJobAsync(string id, PublishJob job)
    {
        var update = Builders<PublishJob>.Update
            .Set(j => j.Status, job.Status)
            .Set(j => j.EndTime, job.EndTime)
            .Set(j => j.Error, job.Error);

        await _db.PublishJobs.UpdateOneAsync(j => j.Id == id, update);
        return await _db.PublishJobs.Find(j => j.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteJobAsync(string id)
    {
        var result = await _db.PublishJobs.DeleteOneAsync(j => j.Id == id);
        await _db.PublishLogs.DeleteManyAsync(l => l.JobId == id);
        return result.DeletedCount > 0;
    }

    public async Task AddLogAsync(PublishLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        await _db.PublishLogs.InsertOneAsync(log);
    }

    public async Task<List<PublishLog>> GetJobLogsAsync(string jobId)
    {
        return await _db.PublishLogs
            .Find(l => l.JobId == jobId)
            .SortBy(l => l.Timestamp)
            .ToListAsync();
    }

    public async Task UpdateProgressAsync(string jobId, PublishProgress progress)
    {
        progress.JobId = jobId;
        progress.LastUpdated = DateTime.UtcNow;

        var update = Builders<PublishJob>.Update.Set(j => j.Progress, progress);
        await _db.PublishJobs.UpdateOneAsync(j => j.Id == jobId, update);
    }

    public async Task<PublishProgress?> GetProgressAsync(string jobId)
    {
        var job = await _db.PublishJobs.Find(j => j.Id == jobId).FirstOrDefaultAsync();
        return job?.Progress;
    }
}
