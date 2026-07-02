using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(IConfiguration configuration, ILogger<ConfigurationController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get all system configuration (non-sensitive fields)
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            var config = BuildConfigResponse();
            return Ok(ApiResponse<object>.SuccessResponse(config, "Configuration retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get Znode API configuration
    /// </summary>
    [HttpGet("znode-api")]
    public IActionResult GetZnodeApi()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            baseUrl = _configuration["ZnodeApi:BaseUrl"] ?? string.Empty,
            userId = _configuration["ZnodeApi:UserId"] ?? "3",
            hasAuthToken = !string.IsNullOrWhiteSpace(_configuration["ZnodeApi:AuthToken"]),
        }, "Znode API configuration"));
    }

    /// <summary>
    /// Get MongoDB configuration (connection string masked)
    /// </summary>
    [HttpGet("mongodb")]
    public IActionResult GetMongoDB()
    {
        var connStr = _configuration["MongoDB:ConnectionString"] ?? string.Empty;
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            connectionString = MaskConnectionString(connStr),
            databaseName = _configuration["MongoDB:DatabaseName"] ?? string.Empty,
            isConfigured = !string.IsNullOrWhiteSpace(connStr),
        }, "MongoDB configuration"));
    }

    /// <summary>
    /// Get health service configuration
    /// </summary>
    [HttpGet("health-service")]
    public IActionResult GetHealthService()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            baseUrl = _configuration["HealthService:BaseUrl"] ?? string.Empty,
            isConfigured = !string.IsNullOrWhiteSpace(_configuration["HealthService:BaseUrl"]),
        }, "Health service configuration"));
    }

    private object BuildConfigResponse()
    {
        var mongoConnStr = _configuration["MongoDB:ConnectionString"] ?? string.Empty;
        var znodeAuthToken = _configuration["ZnodeApi:AuthToken"] ?? string.Empty;
        var healthUrl = _configuration["HealthService:BaseUrl"] ?? string.Empty;
        var publishApiUrl = _configuration["PublishApi:BaseUrl"] ?? string.Empty;
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        return new
        {
            environment = env,
            buildTime = Assembly.GetEntryAssembly()
                            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                            ?.InformationalVersion ?? "unknown",
            server = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.VersionString,
                processorCount = Environment.ProcessorCount,
                runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                workingSetMb = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 1),
            },
            mongodb = new
            {
                connectionString = MaskConnectionString(mongoConnStr),
                databaseName = _configuration["MongoDB:DatabaseName"] ?? string.Empty,
                isConfigured = !string.IsNullOrWhiteSpace(mongoConnStr),
            },
            znodeApi = new
            {
                baseUrl = _configuration["ZnodeApi:BaseUrl"] ?? string.Empty,
                userId = _configuration["ZnodeApi:UserId"] ?? "3",
                hasAuthToken = !string.IsNullOrWhiteSpace(znodeAuthToken),
                tokenPreview = znodeAuthToken.Length > 8
                    ? $"{znodeAuthToken[..4]}...{znodeAuthToken[^4..]}"
                    : string.Empty,
            },
            healthService = new
            {
                baseUrl = healthUrl,
                isConfigured = !string.IsNullOrWhiteSpace(healthUrl),
            },
            publishApi = new
            {
                baseUrl = publishApiUrl,
                isConfigured = !string.IsNullOrWhiteSpace(publishApiUrl),
            },
            cors = new
            {
                allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>(),
            },
            logging = new
            {
                defaultLevel = _configuration["Logging:LogLevel:Default"] ?? "Information",
                aspNetCoreLevel = _configuration["Logging:LogLevel:Microsoft.AspNetCore"] ?? "Warning",
            },
            features = new
            {
                mongoDbEnabled = !string.IsNullOrWhiteSpace(mongoConnStr),
                healthServiceEnabled = !string.IsNullOrWhiteSpace(healthUrl),
                publishApiEnabled = !string.IsNullOrWhiteSpace(publishApiUrl),
            },
        };
    }

    private static string MaskConnectionString(string connStr)
    {
        if (string.IsNullOrWhiteSpace(connStr)) return string.Empty;
        // Mask password if present: mongodb://user:pass@host → mongodb://user:***@host
        var masked = System.Text.RegularExpressions.Regex.Replace(
            connStr,
            @"(?<=://[^:]+:)[^@]+(?=@)",
            "***");
        return masked;
    }
}
