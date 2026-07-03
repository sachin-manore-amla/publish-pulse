namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// Aggregated Elasticsearch cluster configuration retrieved from live APIs.
/// </summary>
public class ElasticsearchConfigurationDto
{
    /// <summary>
    /// Cluster name (e.g., "docker-cluster-znode").
    /// </summary>
    public string ClusterName { get; set; } = "";

    /// <summary>
    /// Elasticsearch server version (e.g., "8.6.2").
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// Cluster health status: green, yellow, or red.
    /// </summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// Number of nodes in the cluster.
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Number of active shards across the cluster.
    /// </summary>
    public int ActiveShardCount { get; set; }

    /// <summary>
    /// Total heap memory used across all nodes (in bytes).
    /// </summary>
    public long HeapUsedBytes { get; set; }

    /// <summary>
    /// Total heap memory available across all nodes (in bytes).
    /// </summary>
    public long HeapMaxBytes { get; set; }

    /// <summary>
    /// Cluster-wide heap usage as a percentage (0–100).
    /// </summary>
    public double HeapUsedPercent { get; set; }
}
