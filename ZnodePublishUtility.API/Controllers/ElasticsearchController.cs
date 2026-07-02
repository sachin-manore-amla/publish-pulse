using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ZnodePublishUtility.API.Controllers;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class EsIndexDto
{
    public string IndexName { get; set; } = "";
    public string Status { get; set; } = "";
    public string Health { get; set; } = "";
    public long DocumentCount { get; set; }
    public int ShardCount { get; set; }
    public int ReplicaCount { get; set; }
    public string IndexSize { get; set; } = "";
    public double IndexSizeGb { get; set; }
}

public class EsNodeStatsDto
{
    public bool Available { get; set; }
    public int NodeCount { get; set; }
    public long TotalHeapMaxMb { get; set; }
    public long TotalHeapUsedMb { get; set; }
    public int HeapUsagePercent { get; set; }
    public long TotalRamMb { get; set; }
    public long TotalDiskGb { get; set; }
    public long AvailableDiskGb { get; set; }
}

public class EsK8sResourceDto
{
    public string PodName { get; set; } = "";
    public string CpuRequest { get; set; } = "";
    public string CpuLimit { get; set; } = "";
    public string MemoryRequest { get; set; } = "";
    public string MemoryLimit { get; set; } = "";
}

public class RecommendedEsConfigDto
{
    public int Shards { get; set; }
    public int Replicas { get; set; }
    public string JvmHeap { get; set; } = "";
    public double JvmHeapGb { get; set; }
    public string Storage { get; set; } = "";
    public double StorageGb { get; set; }
    public int MinNodes { get; set; }
    public string Reasoning { get; set; } = "";
}

public class PublishSummaryDto
{
    public string CatalogId { get; set; } = "";
    public string Environment { get; set; } = "";

    // Publish parameters
    public long ProductCount { get; set; }
    public List<string> Operations { get; set; } = new();

    // Duration estimate
    public int EstimatedMinutes { get; set; }
    public string EstimatedDuration { get; set; } = "";
    public string DurationBasis { get; set; } = "";

    // Current ES index for this catalog
    public bool IndexFound { get; set; }
    public string IndexName { get; set; } = "";
    public string IndexSize { get; set; } = "";
    public double IndexSizeGb { get; set; }
    public long IndexDocCount { get; set; }
    public int IndexShards { get; set; }
    public int IndexReplicas { get; set; }
    public string IndexHealth { get; set; } = "";

    // Estimated index growth after publish
    public double EstimatedNewSizeGb { get; set; }
    public string EstimatedNewSize { get; set; } = "";
    public double EstimatedGrowthGb { get; set; }
    public string EstimatedGrowth { get; set; } = "";

    // All ES indices (for the full list panel)
    public List<EsIndexDto> AllIndices { get; set; } = new();

    // Cluster health
    public string ClusterStatus { get; set; } = "unknown";
    public int ClusterNodes { get; set; }
    public int ActiveShards { get; set; }
    public int UnassignedShards { get; set; }

    // Node-level stats
    public EsNodeStatsDto NodeStats { get; set; } = new();

    // K8s resource limits for ES pods
    public List<EsK8sResourceDto> EsK8sResources { get; set; } = new();

    // Recommended config (calculated from catalog size)
    public RecommendedEsConfigDto Recommended { get; set; } = new();
}

// ── Controller ────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/elasticsearch")]
public class ElasticsearchController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ElasticsearchController> _logger;

    public ElasticsearchController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ElasticsearchController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Aggregated publish summary for a catalog: real ES index data, cluster health,
    /// node stats, K8s resources, recommended config, and estimated publish duration.
    /// </summary>
    [HttpGet("publish-summary/{env}/{catalogId}")]
    public async Task<IActionResult> GetPublishSummary(
        string env,
        string catalogId,
        [FromQuery] long productCount = 0)
    {
        var baseUrl = _configuration["HealthService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Ok(BuildFallbackSummary(catalogId, env, productCount,
                "Health service not configured — showing estimates only"));
        }

        var client = _httpClientFactory.CreateClient("HealthService");

        // Fan out all three health-service calls in parallel
        var indicesTask     = FetchJson(client, $"/api/health/elastic-indices/{env}");
        var nodeStatsTask   = FetchJson(client, $"/api/health/elastic-node-stats/{env}");
        var k8sResTask      = FetchJson(client, $"/api/health/elastic-k8s-resources/{env}");
        var clusterTask     = FetchJson(client, $"/api/health/elastic/{env}");

        await Task.WhenAll(indicesTask, nodeStatsTask, k8sResTask, clusterTask);

        // ── Parse indices ──────────────────────────────────────────────────
        var allIndices = ParseList<EsIndexRaw>(indicesTask.Result)
            .Select(r => new EsIndexDto
            {
                IndexName     = r.IndexName ?? "",
                Status        = r.Status ?? "",
                Health        = r.Health ?? "",
                DocumentCount = r.DocumentCount,
                ShardCount    = r.ShardCount,
                ReplicaCount  = r.ReplicaCount,
                IndexSize     = r.IndexSize ?? "",
                IndexSizeGb   = ParseSizeToGb(r.IndexSize),
            }).ToList();

        // Match this catalog: look for index name containing the catalog ID or "catalog"
        var matched = FindCatalogIndex(allIndices, catalogId);

        // ── Parse node stats ───────────────────────────────────────────────
        var nodeStats = ParseSingle<EsNodeStatsDto>(nodeStatsTask.Result) ?? new EsNodeStatsDto();

        // ── Parse K8s resources ────────────────────────────────────────────
        var k8sResources = ParseList<EsK8sResourceDto>(k8sResTask.Result);

        // ── Parse cluster health ───────────────────────────────────────────
        var cluster = ParseSingle<EsClusterRaw>(clusterTask.Result);

        // ── Compute size estimates ─────────────────────────────────────────
        double bytesPerProduct = matched != null && matched.DocumentCount > 0
            ? (matched.IndexSizeGb * 1024.0 * 1024.0 * 1024.0) / matched.DocumentCount
            : 150_000.0; // 150 KB/product default

        double estimatedNewSizeGb = (productCount * bytesPerProduct) / (1024.0 * 1024.0 * 1024.0);
        double growthGb = Math.Max(0, estimatedNewSizeGb - (matched?.IndexSizeGb ?? 0));

        // ── Compute duration estimate ──────────────────────────────────────
        var (estMinutes, durationStr, durationBasis) = EstimateDuration(
            productCount,
            cluster?.Status,
            nodeStats.HeapUsagePercent,
            matched?.DocumentCount ?? 0);

        // ── Compute recommended config ─────────────────────────────────────
        var recommended = ComputeRecommended(estimatedNewSizeGb, nodeStats, cluster?.ActiveNodes ?? 0);

        var summary = new PublishSummaryDto
        {
            CatalogId         = catalogId,
            Environment       = env,
            ProductCount      = productCount,
            Operations        = new List<string> { "Catalog data sync", "Elasticsearch index build", "Preview & Production revision" },
            EstimatedMinutes  = estMinutes,
            EstimatedDuration = durationStr,
            DurationBasis     = durationBasis,

            IndexFound    = matched != null,
            IndexName     = matched?.IndexName ?? "",
            IndexSize     = matched?.IndexSize ?? "—",
            IndexSizeGb   = matched?.IndexSizeGb ?? 0,
            IndexDocCount = matched?.DocumentCount ?? 0,
            IndexShards   = matched?.ShardCount ?? 0,
            IndexReplicas = matched?.ReplicaCount ?? 0,
            IndexHealth   = matched?.Health ?? "unknown",

            EstimatedNewSizeGb = estimatedNewSizeGb,
            EstimatedNewSize   = FormatGb(estimatedNewSizeGb),
            EstimatedGrowthGb  = growthGb,
            EstimatedGrowth    = FormatGb(growthGb),

            AllIndices = allIndices,

            ClusterStatus    = cluster?.Status ?? "unknown",
            ClusterNodes     = cluster?.ActiveNodes ?? 0,
            ActiveShards     = cluster?.ActiveShards ?? 0,
            UnassignedShards = cluster?.UnassignedShards ?? 0,

            NodeStats      = nodeStats,
            EsK8sResources = k8sResources,
            Recommended    = recommended,
        };

        return Ok(summary);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EsIndexDto? FindCatalogIndex(List<EsIndexDto> indices, string catalogId)
    {
        if (!indices.Any()) return null;

        // Exact numeric ID in the index name (e.g. "catalog_5", "znodecatalog_5")
        var byId = indices.FirstOrDefault(i =>
            i.IndexName.Contains(catalogId, StringComparison.OrdinalIgnoreCase));
        if (byId != null) return byId;

        // Fall back: any index with "catalog" in the name, prefer largest
        return indices
            .Where(i => i.IndexName.Contains("catalog", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.IndexSizeGb)
            .FirstOrDefault();
    }

    private static (int minutes, string label, string basis) EstimateDuration(
        long productCount,
        string? clusterStatus,
        int heapPct,
        long existingDocCount)
    {
        double baseRate = 2000; // products per minute on healthy cluster

        // Cluster health factor
        double healthFactor = clusterStatus switch
        {
            "green"  => 1.0,
            "yellow" => 0.80,
            "red"    => 0.55,
            _        => 0.85,
        };

        // JVM heap pressure factor
        double heapFactor = heapPct switch
        {
            < 60  => 1.0,
            < 75  => 0.90,
            < 85  => 0.75,
            _     => 0.60,
        };

        double adjustedRate = baseRate * healthFactor * heapFactor;
        double indexingMin  = productCount > 0 ? productCount / adjustedRate : 1;
        double overhead     = 3; // warmup + finalize
        int total           = (int)Math.Ceiling(indexingMin + overhead);

        string label = total < 60
            ? $"~{total} min"
            : $"~{total / 60}h {total % 60}min";

        var factors = new List<string>();
        if (clusterStatus is "yellow" or "red") factors.Add($"cluster {clusterStatus}");
        if (heapPct > 75) factors.Add($"heap {heapPct}%");
        string basis = factors.Any()
            ? $"Adjusted for: {string.Join(", ", factors)}"
            : "Based on healthy cluster rate (2 000 products/min)";

        return (total, label, basis);
    }

    private static RecommendedEsConfigDto ComputeRecommended(
        double indexSizeGb,
        EsNodeStatsDto nodeStats,
        int currentNodes)
    {
        double sizeGb = Math.Max(indexSizeGb, 0.1);

        // Shards: 1 per 30 GB primary, minimum 1
        int shards = Math.Max(1, (int)Math.Ceiling(sizeGb / 30));

        // Replicas: 1 for production HA
        int replicas = 1;

        // JVM heap: 1.5× index size, capped at 30 GB (ES recommendation)
        double jvmGb = Math.Min(Math.Ceiling(sizeGb * 1.5), 30);

        // Storage: (primary + replicas) × size × 1.2 overhead factor
        double storageGb = sizeGb * (replicas + 1) * 1.2;

        // Nodes: enough to spread shards, minimum 3 for HA
        int minNodes = Math.Max(3, replicas + 1);

        var reasons = new List<string>
        {
            $"Index ~{FormatGb(sizeGb)}",
            $"{shards} shard(s) × 30 GB limit",
            $"JVM capped at 30 GB per node",
        };
        if (currentNodes > 0 && currentNodes < minNodes)
            reasons.Add($"Current cluster has {currentNodes} node(s), recommend {minNodes}");

        return new RecommendedEsConfigDto
        {
            Shards     = shards,
            Replicas   = replicas,
            JvmHeapGb  = jvmGb,
            JvmHeap    = $"{jvmGb:F0} GB",
            StorageGb  = storageGb,
            Storage    = FormatGb(storageGb),
            MinNodes   = minNodes,
            Reasoning  = string.Join(" | ", reasons),
        };
    }

    private static PublishSummaryDto BuildFallbackSummary(
        string catalogId, string env, long productCount, string note)
    {
        double indexSizeGb = (productCount / 40_000.0) * 4.0;
        int estMin = (int)Math.Ceiling(productCount / 2000.0) + 3;
        return new PublishSummaryDto
        {
            CatalogId         = catalogId,
            Environment       = env,
            ProductCount      = productCount,
            Operations        = new List<string> { "Catalog data sync", "Elasticsearch index build", "Revision publish" },
            EstimatedMinutes  = estMin,
            EstimatedDuration = $"~{estMin} min",
            DurationBasis     = note,
            IndexFound        = false,
            EstimatedNewSizeGb = indexSizeGb,
            EstimatedNewSize   = FormatGb(indexSizeGb),
            ClusterStatus      = "unknown",
            NodeStats          = new EsNodeStatsDto { Available = false },
            Recommended        = ComputeRecommended(indexSizeGb, new EsNodeStatsDto(), 0),
        };
    }

    // ── Size helpers ──────────────────────────────────────────────────────────

    private static double ParseSizeToGb(string? size)
    {
        if (string.IsNullOrWhiteSpace(size)) return 0;
        size = size.Trim().ToLowerInvariant();
        if (size.EndsWith("tb")) return double.TryParse(size[..^2], out var tb) ? tb * 1024 : 0;
        if (size.EndsWith("gb")) return double.TryParse(size[..^2], out var gb) ? gb : 0;
        if (size.EndsWith("mb")) return double.TryParse(size[..^2], out var mb) ? mb / 1024 : 0;
        if (size.EndsWith("kb")) return double.TryParse(size[..^2], out var kb) ? kb / 1024 / 1024 : 0;
        if (size.EndsWith("b"))  return double.TryParse(size[..^1],  out var b)  ? b  / 1024 / 1024 / 1024 : 0;
        return 0;
    }

    private static string FormatGb(double gb) =>
        gb >= 1 ? $"{gb:F2} GB" : $"{gb * 1024:F1} MB";

    // ── HTTP fetch helpers ────────────────────────────────────────────────────

    private async Task<string?> FetchJson(HttpClient client, string path)
    {
        try
        {
            var resp = await client.GetAsync(path);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Elasticsearch proxy fetch failed for {Path}: {Msg}", path, ex.Message);
            return null;
        }
    }

    private static List<T> ParseList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        try { return JsonSerializer.Deserialize<List<T>>(json, _jsonOpts) ?? new List<T>(); }
        catch { return new List<T>(); }
    }

    private static T? ParseSingle<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<T>(json, _jsonOpts); }
        catch { return null; }
    }

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Raw shapes from health-service JSON (camelCase matching)
    private class EsIndexRaw
    {
        public string? IndexName { get; set; }
        public string? Status { get; set; }
        public string? Health { get; set; }
        public long DocumentCount { get; set; }
        public int ShardCount { get; set; }
        public int ReplicaCount { get; set; }
        public string? IndexSize { get; set; }
    }

    private class EsClusterRaw
    {
        public string? Status { get; set; }
        public string? ClusterName { get; set; }
        public int ActiveNodes { get; set; }
        public int TotalNodes { get; set; }
        public int ActiveShards { get; set; }
        public int UnassignedShards { get; set; }
    }
}
