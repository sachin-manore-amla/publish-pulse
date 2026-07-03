using System.Text.Json;
using ZnodePublishUtility.API.Infrastructure.Elasticsearch;

namespace ZnodePublishUtility.API.Tests.Infrastructure.Elasticsearch;

public class ElasticsearchConfigurationAggregatorTests
{
    private const string HealthJson = """
        {
            "cluster_name": "znode-cluster",
            "status": "green",
            "number_of_nodes": 2,
            "active_shards": 2
        }
        """;

    private const string NodesWithFullStatsJson = """
        {
            "nodes": {
                "node-1": {
                    "name": "node-1",
                    "version": "8.11.0",
                    "jvm": { "mem": { "heap_max_in_bytes": 2147483648, "heap_used_in_bytes": 1073741824 } }
                },
                "node-2": {
                    "name": "node-2",
                    "version": "8.11.0",
                    "jvm": { "mem": { "heap_max_in_bytes": 2147483648, "heap_used_in_bytes": 536870912 } }
                }
            }
        }
        """;

    private const string NodesInfoOnlyJson = """
        {
            "nodes": {
                "node-1": {
                    "name": "node-1",
                    "version": "8.11.0",
                    "jvm": { "mem": { "heap_max_in_bytes": 2147483648 } }
                }
            }
        }
        """;

    private const string NodesEmptyJson = """{ "nodes": {} }""";

    private const string ShardsMixedStatesJson = """
        [
            { "index": "catalog_1", "shard": "0", "state": "STARTED" },
            { "index": "catalog_1", "shard": "1", "state": "started" },
            { "index": "catalog_1", "shard": "2", "state": "UNASSIGNED" },
            { "index": "catalog_1", "shard": "3", "state": "RELOCATING" }
        ]
        """;

    private const string ShardsEmptyJson = "[]";

    [Fact]
    public void Aggregate_HappyPath_ComputesAllFieldsFromRawResponses()
    {
        using var health = JsonDocument.Parse(HealthJson);
        using var nodes = JsonDocument.Parse(NodesWithFullStatsJson);
        using var shards = JsonDocument.Parse(ShardsMixedStatesJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        Assert.Equal("znode-cluster", result.ClusterName);
        Assert.Equal("green", result.Status);
        Assert.Equal("8.11.0", result.Version);
        Assert.Equal(2, result.NodeCount);
        Assert.Equal(1610612736, result.HeapUsedBytes); // 1_073_741_824 + 536_870_912
        Assert.Equal(4294967296, result.HeapMaxBytes);  // 2 * 2_147_483_648
        Assert.Equal(37.5, result.HeapUsedPercent);
    }

    [Fact]
    public void Aggregate_CountsOnlyStartedShards_CaseInsensitively()
    {
        using var health = JsonDocument.Parse(HealthJson);
        using var nodes = JsonDocument.Parse(NodesWithFullStatsJson);
        using var shards = JsonDocument.Parse(ShardsMixedStatesJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        // "STARTED" and "started" count; "UNASSIGNED" and "RELOCATING" do not.
        Assert.Equal(2, result.ActiveShardCount);
    }

    [Fact]
    public void Aggregate_NoShards_ReturnsZeroActiveShardCount()
    {
        using var health = JsonDocument.Parse(HealthJson);
        using var nodes = JsonDocument.Parse(NodesWithFullStatsJson);
        using var shards = JsonDocument.Parse(ShardsEmptyJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        Assert.Equal(0, result.ActiveShardCount);
    }

    [Fact]
    public void Aggregate_NodesInfoWithoutHeapUsed_DefaultsHeapUsedToZeroWithoutThrowing()
    {
        using var health = JsonDocument.Parse(HealthJson);
        using var nodes = JsonDocument.Parse(NodesInfoOnlyJson);
        using var shards = JsonDocument.Parse(ShardsEmptyJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        Assert.Equal(1, result.NodeCount);
        Assert.Equal(2147483648, result.HeapMaxBytes);
        Assert.Equal(0, result.HeapUsedBytes);
        Assert.Equal(0, result.HeapUsedPercent);
    }

    [Fact]
    public void Aggregate_NoNodes_AvoidsDivideByZeroAndReturnsZeroedStats()
    {
        using var health = JsonDocument.Parse(HealthJson);
        using var nodes = JsonDocument.Parse(NodesEmptyJson);
        using var shards = JsonDocument.Parse(ShardsEmptyJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        Assert.Equal(0, result.NodeCount);
        Assert.Equal(0, result.HeapMaxBytes);
        Assert.Equal(0, result.HeapUsedBytes);
        Assert.Equal(0, result.HeapUsedPercent);
        Assert.Equal("", result.Version);
    }

    [Fact]
    public void Aggregate_UsesClusterNameAndStatusFromHealthResponse()
    {
        const string yellowHealthJson = """{ "cluster_name": "other-cluster", "status": "yellow" }""";
        using var health = JsonDocument.Parse(yellowHealthJson);
        using var nodes = JsonDocument.Parse(NodesEmptyJson);
        using var shards = JsonDocument.Parse(ShardsEmptyJson);

        var result = ElasticsearchConfigurationAggregator.Aggregate(health, nodes, shards);

        Assert.Equal("other-cluster", result.ClusterName);
        Assert.Equal("yellow", result.Status);
    }
}
