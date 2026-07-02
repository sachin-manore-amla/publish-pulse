using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

/// <summary>
/// Proxies all health / pod / Kubernetes status requests to the publish_health service.
/// The frontend calls this BFF which forwards to the dedicated health service.
/// </summary>
[ApiController]
[Route("api/system-health")]
public class HealthProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthProxyController> _logger;

    public HealthProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HealthProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// List available Kubernetes environments
    /// </summary>
    [HttpGet("environments")]
    public async Task<IActionResult> GetEnvironments()
        => await ProxyGet("/api/health/environments");

    /// <summary>
    /// Get pod health for an environment (with CPU/Memory metrics, per-pod category)
    /// </summary>
    [HttpGet("pods/{env}")]
    public async Task<IActionResult> GetPods(string env)
        => await ProxyGet($"/api/health/pods/{env}");

    /// <summary>
    /// Get Elasticsearch cluster health for an environment
    /// </summary>
    [HttpGet("elastic/{env}")]
    public async Task<IActionResult> GetElasticHealth(string env)
        => await ProxyGet($"/api/health/elastic/{env}");

    /// <summary>
    /// Get Kubernetes node status for an environment
    /// </summary>
    [HttpGet("nodes/{env}")]
    public async Task<IActionResult> GetNodes(string env)
        => await ProxyGet($"/api/health/nodes/{env}");

    /// <summary>
    /// Get Kubernetes events for an environment (optionally filter by type=warning)
    /// </summary>
    [HttpGet("events/{env}")]
    public async Task<IActionResult> GetEvents(string env, [FromQuery] string type = "all")
        => await ProxyGet($"/api/health/events/{env}?type={Uri.EscapeDataString(type)}");

    /// <summary>
    /// Get all Elasticsearch indices for an environment (size, doc count, shards, replicas)
    /// </summary>
    [HttpGet("elastic-indices/{env}")]
    public async Task<IActionResult> GetElasticIndices(string env)
        => await ProxyGet($"/api/health/elastic-indices/{env}");

    /// <summary>
    /// Get Elasticsearch node-level stats: JVM heap, OS memory, filesystem usage
    /// </summary>
    [HttpGet("elastic-node-stats/{env}")]
    public async Task<IActionResult> GetElasticNodeStats(string env)
        => await ProxyGet($"/api/health/elastic-node-stats/{env}");

    /// <summary>
    /// Get K8s resource limits/requests for Elasticsearch pods only
    /// </summary>
    [HttpGet("elastic-k8s-resources/{env}")]
    public async Task<IActionResult> GetElasticK8sResources(string env)
        => await ProxyGet($"/api/health/elastic-k8s-resources/{env}");

    /// <summary>
    /// Check if the health service itself is reachable
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var baseUrl = _configuration["HealthService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Ok(new
            {
                available = false,
                message = "Health service URL not configured (HealthService:BaseUrl)",
                configuredUrl = string.Empty,
            });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("HealthService");
            var response = await client.GetAsync("/api/health/environments");
            return Ok(new
            {
                available = response.IsSuccessStatusCode,
                httpStatus = (int)response.StatusCode,
                configuredUrl = baseUrl,
                message = response.IsSuccessStatusCode
                    ? "Health service is reachable"
                    : $"Health service returned {(int)response.StatusCode}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health service unreachable at {BaseUrl}", baseUrl);
            return Ok(new
            {
                available = false,
                configuredUrl = baseUrl,
                message = $"Health service unreachable: {ex.Message}",
            });
        }
    }

    // ── Private proxy helper ──────────────────────────────────────────────────

    private async Task<IActionResult> ProxyGet(string path)
    {
        var baseUrl = _configuration["HealthService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Ok(new { available = false, message = "Health service URL not configured" });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("HealthService");
            var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Health proxy {Path} returned {Status}", path, (int)response.StatusCode);
                return StatusCode((int)response.StatusCode,
                    ApiResponse.ErrorResponse($"Health service returned {(int)response.StatusCode}"));
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health proxy error for {Path}", path);
            return StatusCode(503, ApiResponse.ErrorResponse($"Health service unavailable: {ex.Message}"));
        }
    }
}
