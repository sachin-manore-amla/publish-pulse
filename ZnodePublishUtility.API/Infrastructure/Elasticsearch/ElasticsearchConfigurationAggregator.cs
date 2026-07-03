using System.Text.Json;

namespace ZnodePublishUtility.API.Infrastructure.Elasticsearch;

/// <summary>
/// Aggregates responses from multiple Elasticsearch APIs into a unified configuration DTO.
/// </summary>
public static class ElasticsearchConfigurationAggregator
{
    /// <summary>
    /// Aggregate cluster health, nodes, and shards data into a configuration DTO.
    /// </summary>
    public static ElasticsearchConfigurationDto Aggregate(
        JsonDocument healthDoc,
        JsonDocument nodesDoc,
        JsonDocument shardsDoc)
    {
        var health = healthDoc.RootElement;
        var nodes = nodesDoc.RootElement;
        var shards = shardsDoc.RootElement;

        var dto = new ElasticsearchConfigurationDto();

        // Extract from health response
        if (health.TryGetProperty("cluster_name", out var clusterNameElement))
            dto.ClusterName = clusterNameElement.GetString() ?? "";

        if (health.TryGetProperty("status", out var statusElement))
            dto.Status = statusElement.GetString() ?? "";

        // Extract node count and version from nodes response
        if (nodes.TryGetProperty("nodes", out var nodesObject))
        {
            var nodeList = nodesObject.EnumerateObject().ToList();
            dto.NodeCount = nodeList.Count;

            // Get version from first node
            if (nodeList.Count > 0)
            {
                var firstNode = nodeList[0].Value;
                if (firstNode.TryGetProperty("version", out var versionElement))
                    dto.Version = versionElement.GetString() ?? "";

                // Aggregate heap from all nodes
                long totalHeapUsed = 0;
                long totalHeapMax = 0;

                foreach (var node in nodeList)
                {
                    if (node.Value.TryGetProperty("jvm", out var jvmElement) &&
                        jvmElement.TryGetProperty("mem", out var memElement))
                    {
                        if (memElement.TryGetProperty("heap_used_in_bytes", out var usedElement))
                            totalHeapUsed += usedElement.GetInt64();

                        if (memElement.TryGetProperty("heap_max_in_bytes", out var maxElement))
                            totalHeapMax += maxElement.GetInt64();
                    }
                }

                dto.HeapUsedBytes = totalHeapUsed;
                dto.HeapMaxBytes = totalHeapMax;
                dto.HeapUsedPercent = totalHeapMax > 0
                    ? Math.Round((double)totalHeapUsed / totalHeapMax * 100, 1)
                    : 0;
            }
        }

        // Count active shards
        if (shards.ValueKind == JsonValueKind.Array)
        {
            int activeCount = 0;
            foreach (var shard in shards.EnumerateArray())
            {
                if (shard.TryGetProperty("state", out var stateElement))
                {
                    var state = stateElement.GetString() ?? "";
                    if (state.Equals("STARTED", StringComparison.OrdinalIgnoreCase))
                        activeCount++;
                }
            }
            dto.ActiveShardCount = activeCount;
        }

        return dto;
    }
}
