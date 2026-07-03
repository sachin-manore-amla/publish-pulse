using System.Text.Json;

namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// Client for communicating with Elasticsearch cluster APIs.
/// </summary>
public interface IElasticsearchClient
{
    /// <summary>
    /// Retrieve cluster health status (GET /_cluster/health).
    /// </summary>
    Task<JsonDocument> GetClusterHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve node information and stats (GET /_nodes?format=json).
    /// </summary>
    Task<JsonDocument> GetNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve shard allocation information (GET /_cat/shards?format=json).
    /// </summary>
    Task<JsonDocument> GetShardsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve cluster settings (GET /_cluster/settings).
    /// </summary>
    Task<JsonDocument> GetClusterSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve index mapping (GET /{indexName}/_mapping).
    /// </summary>
    Task<JsonDocument> GetIndexMappingAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve document count, store size and health/status for a single index
    /// (GET /_cat/indices/{indexName}?format=json&amp;bytes=b).
    /// </summary>
    Task<JsonDocument> GetIndexStatsAsync(string indexName, CancellationToken cancellationToken = default);
}
