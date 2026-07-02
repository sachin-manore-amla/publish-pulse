using Microsoft.AspNetCore.Mvc;
using ZnodeSphere.Interfaces;
using ZnodeSphere.Models;
using publish_health;

namespace publish_health.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IKubeService _kubeService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IKubeService kubeService, ILogger<HealthController> logger)
        {
            _kubeService = kubeService;
            _logger = logger;
        }

        // GET /api/health/environments
        [HttpGet("environments")]
        public async Task<IActionResult> GetEnvironments()
        {
            var envs = await _kubeService.GetEnvironmentsAsync();
            return Ok(envs);
        }

        // GET /api/health/pods/{env}  — pods merged with CPU/Memory metrics, each pod categorised
        [HttpGet("pods/{env}")]
        public async Task<IActionResult> GetPods(string env)
        {
            var podsTask   = _kubeService.GetPodsAsync(env, excludeNonAppServices: false);
            var metricsTask = _kubeService.GetPodMetricsAsync(env);

            await Task.WhenAll(podsTask, metricsTask);

            var pods    = podsTask.Result;
            var metrics = metricsTask.Result;

            var metricsMap = metrics.ToDictionary(
                m => m.PodName,
                m => m,
                StringComparer.OrdinalIgnoreCase);

            var result = pods.Select(p =>
            {
                metricsMap.TryGetValue(p.Name, out var m);
                return new PodHealthDto
                {
                    Name             = p.Name,
                    Status           = p.Status,
                    IpAddress        = p.IpAddress,
                    NodeName         = p.NodeName,
                    RestartCount     = p.RestartCount,
                    AgeDays          = p.AgeDays,
                    TotalContainers  = p.TotalContainers,
                    ReadyContainers  = p.ReadyContainers,
                    ContainerReason  = p.ContainerReason,
                    Cpu              = m?.Cpu ?? "—",
                    Memory           = m?.Memory ?? "—",
                    CpuCores         = m?.CpuCores ?? 0,
                    MemoryMi         = m?.MemoryMi ?? 0,
                    Category         = CategorizePod(p.Name)
                };
            }).ToList();

            int running  = result.Count(p => p.Status == "Running");
            int degraded = result.Count(p => p.Status == "Degraded");
            int failed   = result.Count(p => p.Status is "Failed" or "CrashLoopBackOff" or "Error");
            int pending  = result.Count(p => p.Status == "Pending");

            return Ok(new
            {
                environment   = env,
                lastRefreshed = DateTime.UtcNow,
                summary = new { total = result.Count, running, degraded, failed, pending },
                pods    = result
            });
        }

        // GET /api/health/elastic/{env}
        [HttpGet("elastic/{env}")]
        public async Task<IActionResult> GetElasticHealth(string env)
        {
            var health = await _kubeService.GetElasticClusterHealthAsync(env);
            return Ok(health);
        }

        // GET /api/health/nodes/{env}
        [HttpGet("nodes/{env}")]
        public async Task<IActionResult> GetNodes(string env)
        {
            var nodes = await _kubeService.GetNodesAsync(env);
            return Ok(nodes);
        }

        // GET /api/health/events/{env}?type=warning
        [HttpGet("events/{env}")]
        public async Task<IActionResult> GetEvents(string env, [FromQuery] string type = "all")
        {
            var events = await _kubeService.GetEventsAsync(env, type);
            return Ok(events);
        }

        // GET /api/health/elastic-indices/{env}
        [HttpGet("elastic-indices/{env}")]
        public async Task<IActionResult> GetElasticIndices(string env)
        {
            var indices = await _kubeService.GetElasticIndicesAsync(env);
            return Ok(indices);
        }

        // GET /api/health/elastic-node-stats/{env}
        [HttpGet("elastic-node-stats/{env}")]
        public async Task<IActionResult> GetElasticNodeStats(string env)
        {
            var stats = await _kubeService.GetElasticNodeStatsAsync(env);
            return Ok(stats);
        }

        // GET /api/health/elastic-k8s-resources/{env}  — ES pod CPU/memory limits
        [HttpGet("elastic-k8s-resources/{env}")]
        public async Task<IActionResult> GetElasticK8sResources(string env)
        {
            var resources = await _kubeService.GetResourceLimitsAsync(env);
            var esOnly = resources
                .Where(r => r.PodName.Contains("elastic", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Ok(esOnly);
        }

        private static string CategorizePod(string name)
        {
            var n = name.ToLowerInvariant();

            if (n.Contains("mssql") || n.Contains("sqlserver") || n.Contains("database") ||
                n.Contains("sql-") || n.Contains("-sql") || n.Contains("db-") || n.Contains("-db") ||
                n.Contains("mongodb") || n.Contains("postgres") || n.Contains("mysql") ||
                n.Contains("redis") || n.Contains("rabbitmq"))
                return "database";

            if (n.Contains("elasticsearch") || n.Contains("elastic") || n.Contains("kibana") ||
                n.StartsWith("es-") || n.Contains("-es-") || n.EndsWith("-es"))
                return "elastic";

            if (n.Contains("publish") || n.Contains("utility") || n.Contains("worker") ||
                n.Contains("queue") || n.Contains("scheduler") || n.Contains("hangfire") ||
                n.Contains("job") && !n.Contains("injob"))
                return "publish";

            return "other";
        }
    }

    public class PodHealthDto
    {
        public string Name            { get; set; }
        public string Status          { get; set; }
        public string IpAddress       { get; set; }
        public string NodeName        { get; set; }
        public int    RestartCount    { get; set; }
        public int    AgeDays         { get; set; }
        public int    TotalContainers { get; set; }
        public int    ReadyContainers { get; set; }
        public string ContainerReason { get; set; }
        public string Cpu             { get; set; }
        public string Memory          { get; set; }
        public double CpuCores        { get; set; }
        public long   MemoryMi        { get; set; }
        public string Category        { get; set; }
    }
}
