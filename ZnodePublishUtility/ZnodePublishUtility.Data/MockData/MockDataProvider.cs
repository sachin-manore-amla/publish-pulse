using ZnodePublishUtility.Models;

namespace ZnodePublishUtility.Data.MockData;

public static class MockDataProvider
{
    public static List<Catalog> GetMockCatalogs()
    {
        return new List<Catalog>
        {
            new() { Id = "cat-1", Name = "Maxwell", ProductCount = 15420, LastPublished = DateTime.Parse("2024-01-15T10:30:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-01T00:00:00Z") },
            new() { Id = "cat-2", Name = "Seasonal Collection", ProductCount = 3250, LastPublished = DateTime.Parse("2024-01-14T15:45:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-02T00:00:00Z") },
            new() { Id = "cat-3", Name = "B2B Wholesale Catalog", ProductCount = 8900, LastPublished = DateTime.Parse("2024-01-12T09:00:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-03T00:00:00Z") },
            new() { Id = "cat-4", Name = "Clearance Items", ProductCount = 1200, LastPublished = DateTime.Parse("2024-01-10T14:20:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-04T00:00:00Z") },
            new() { Id = "cat-5", Name = "New Arrivals 2024", ProductCount = 580, LastPublished = null, CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-05T00:00:00Z") },
        };
    }

    public static List<Store> GetMockStores()
    {
        return new List<Store>
        {
            new() { Id = "store-1", Name = "US Flagship Store", Domain = "us.example.com", LastPublished = DateTime.Parse("2024-01-15T08:00:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-01T00:00:00Z") },
            new() { Id = "store-2", Name = "EU Store", Domain = "eu.example.com", LastPublished = DateTime.Parse("2024-01-14T12:00:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-02T00:00:00Z") },
            new() { Id = "store-3", Name = "UK Store", Domain = "uk.example.com", LastPublished = DateTime.Parse("2024-01-13T16:30:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-03T00:00:00Z") },
            new() { Id = "store-4", Name = "APAC Store", Domain = "apac.example.com", LastPublished = null, CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-04T00:00:00Z") },
        };
    }

    public static List<Portal> GetMockPortals()
    {
        return new List<Portal>
        {
            new() { Id = "portal-1", Name = "Main Website", StoreId = "store-1", StoreName = "US Flagship Store", LastPublished = DateTime.Parse("2024-01-15T11:00:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-01T00:00:00Z") },
            new() { Id = "portal-2", Name = "Customer Portal", StoreId = "store-1", StoreName = "US Flagship Store", LastPublished = DateTime.Parse("2024-01-14T09:30:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-02T00:00:00Z") },
            new() { Id = "portal-3", Name = "Partner Portal", StoreId = "store-2", StoreName = "EU Store", LastPublished = null, CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-03T00:00:00Z") },
            new() { Id = "portal-4", Name = "Mobile Landing", StoreId = "store-3", StoreName = "UK Store", LastPublished = DateTime.Parse("2024-01-12T14:00:00Z"), CreatedBy = "admin", IsActive = true, CreatedAt = DateTime.Parse("2024-01-04T00:00:00Z") },
        };
    }

    public static List<PublishJob> GetMockPublishJobs()
    {
        return new List<PublishJob>();
    }

    public static List<PublishHistory> GetMockPublishHistory()
    {
        return new List<PublishHistory>
        {
            new()
            {
                Id = "hist-1",
                Type = PublishType.Catalog,
                TargetName = "Main Product Catalog",
                Status = PublishStatus.Completed,
                StartTime = DateTime.Parse("2024-01-15T10:30:00Z"),
                EndTime = DateTime.Parse("2024-01-15T10:45:00Z"),
                Duration = "15m 23s",
                Details = new PublishHistoryDetail { TotalRecords = 15420, ProcessedRecords = 15420, FailedRecords = 0 }
            },
            new()
            {
                Id = "hist-2",
                Type = PublishType.Store,
                TargetName = "US Flagship Store",
                Status = PublishStatus.Completed,
                StartTime = DateTime.Parse("2024-01-15T08:00:00Z"),
                EndTime = DateTime.Parse("2024-01-15T08:12:00Z"),
                Duration = "12m 45s",
                Details = new PublishHistoryDetail { TotalRecords = 245, ProcessedRecords = 245, FailedRecords = 0 }
            },
            new()
            {
                Id = "hist-3",
                Type = PublishType.CMS,
                TargetName = "Main Website",
                Status = PublishStatus.Failed,
                StartTime = DateTime.Parse("2024-01-14T16:00:00Z"),
                EndTime = DateTime.Parse("2024-01-14T16:05:00Z"),
                Duration = "5m 12s",
                Error = new PublishError
                {
                    Message = "Template rendering failed: Missing asset reference in header.html line 45",
                    Stage = "Template Rendering",
                    CorrelationId = "err-cms-20240114-160512"
                }
            }
        };
    }
}
