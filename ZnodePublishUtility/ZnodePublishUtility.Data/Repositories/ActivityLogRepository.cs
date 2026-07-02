using MongoDB.Bson;
using MongoDB.Driver;
using ZnodePublishUtility.Data.Interfaces;
using ZnodePublishUtility.Data.MongoDB;
using ZnodePublishUtility.Models;
using ZnodePublishUtility.Models.DTOs;

namespace ZnodePublishUtility.Data.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly MongoDbContext _db;

    public ActivityLogRepository(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<ActivityLog> CreateAsync(ActivityLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        await _db.ActivityLogs.InsertOneAsync(log);
        return log;
    }

    public async Task<List<ActivityLog>> GetAllAsync(ActivityLogFilterDto filter)
    {
        var fb = Builders<ActivityLog>.Filter;
        var filters = new List<FilterDefinition<ActivityLog>>();

        if (!string.IsNullOrWhiteSpace(filter.Level))
            filters.Add(fb.Eq(a => a.Level, filter.Level));
        if (!string.IsNullOrWhiteSpace(filter.Source))
            filters.Add(fb.Eq(a => a.Source, filter.Source));
        if (!string.IsNullOrWhiteSpace(filter.Action))
            filters.Add(fb.Eq(a => a.Action, filter.Action));
        if (!string.IsNullOrWhiteSpace(filter.JobId))
            filters.Add(fb.Eq(a => a.JobId, filter.JobId));
        if (filter.From.HasValue)
            filters.Add(fb.Gte(a => a.Timestamp, filter.From.Value));
        if (filter.To.HasValue)
            filters.Add(fb.Lte(a => a.Timestamp, filter.To.Value));
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchFilter = fb.Or(
                fb.Regex(a => a.Message, new BsonRegularExpression(filter.Search, "i")),
                fb.Regex(a => a.Details, new BsonRegularExpression(filter.Search, "i")),
                fb.Regex(a => a.TargetName, new BsonRegularExpression(filter.Search, "i"))
            );
            filters.Add(searchFilter);
        }

        var combined = filters.Count > 0 ? fb.And(filters) : fb.Empty;
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 500);

        return await _db.ActivityLogs
            .Find(combined)
            .SortByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(ActivityLogFilterDto filter)
    {
        var fb = Builders<ActivityLog>.Filter;
        var filters = new List<FilterDefinition<ActivityLog>>();

        if (!string.IsNullOrWhiteSpace(filter.Level)) filters.Add(fb.Eq(a => a.Level, filter.Level));
        if (!string.IsNullOrWhiteSpace(filter.Source)) filters.Add(fb.Eq(a => a.Source, filter.Source));
        if (!string.IsNullOrWhiteSpace(filter.Action)) filters.Add(fb.Eq(a => a.Action, filter.Action));
        if (!string.IsNullOrWhiteSpace(filter.JobId)) filters.Add(fb.Eq(a => a.JobId, filter.JobId));
        if (filter.From.HasValue) filters.Add(fb.Gte(a => a.Timestamp, filter.From.Value));
        if (filter.To.HasValue) filters.Add(fb.Lte(a => a.Timestamp, filter.To.Value));
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            filters.Add(fb.Or(
                fb.Regex(a => a.Message, new BsonRegularExpression(filter.Search, "i")),
                fb.Regex(a => a.Details, new BsonRegularExpression(filter.Search, "i"))
            ));
        }

        var combined = filters.Count > 0 ? fb.And(filters) : fb.Empty;
        return (int)await _db.ActivityLogs.CountDocumentsAsync(combined);
    }

    public async Task<ActivityLog?> GetByIdAsync(string id)
    {
        return await _db.ActivityLogs.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ActivityLogStatsDto> GetStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var todayFilter = Builders<ActivityLog>.Filter.Gte(a => a.Timestamp, today);

        var allLogs = await _db.ActivityLogs.Find(_ => true).ToListAsync();
        var todayLogs = allLogs.Where(l => l.Timestamp >= today).ToList();

        var bySource = allLogs
            .GroupBy(l => l.Source)
            .ToDictionary(g => g.Key ?? "unknown", g => g.Count());

        return new ActivityLogStatsDto
        {
            TotalLogs = allLogs.Count,
            ErrorCount = allLogs.Count(l => l.Level == "error"),
            WarningCount = allLogs.Count(l => l.Level == "warning"),
            InfoCount = allLogs.Count(l => l.Level == "info"),
            SuccessCount = allLogs.Count(l => l.Level == "success"),
            PublishStartedToday = todayLogs.Count(l => l.Action == "publish_started"),
            PublishCompletedToday = todayLogs.Count(l => l.Action == "publish_completed"),
            PublishFailedToday = todayLogs.Count(l => l.Action == "publish_failed"),
            BySource = bySource,
            LastActivity = allLogs.OrderByDescending(l => l.Timestamp).FirstOrDefault()?.Timestamp,
        };
    }

    public async Task<List<ActivityLog>> GetRecentAsync(int limit = 100)
    {
        return await _db.ActivityLogs
            .Find(_ => true)
            .SortByDescending(a => a.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByJobIdAsync(string jobId)
    {
        return await _db.ActivityLogs
            .Find(a => a.JobId == jobId)
            .SortByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        await _db.ActivityLogs.DeleteManyAsync(a => a.Timestamp < cutoff);
    }

    // ── Publish log specific ──────────────────────────────────────────────────

    public async Task<PublishLog> CreatePublishLogAsync(PublishLog log)
    {
        log.Timestamp = DateTime.UtcNow;
        await _db.PublishLogs.InsertOneAsync(log);
        return log;
    }

    public async Task<List<PublishLog>> GetPublishLogsByJobIdAsync(string jobId)
    {
        return await _db.PublishLogs
            .Find(l => l.JobId == jobId)
            .SortBy(l => l.Timestamp)
            .ToListAsync();
    }

    public async Task BulkCreatePublishLogsAsync(List<PublishLog> logs)
    {
        if (logs.Count == 0) return;
        var ts = DateTime.UtcNow;
        logs.ForEach(l => { if (l.Timestamp == default) l.Timestamp = ts; });
        await _db.PublishLogs.InsertManyAsync(logs);
    }
}
