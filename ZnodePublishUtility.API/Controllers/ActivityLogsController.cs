using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _service;
    private readonly ILogger<ActivityLogsController> _logger;

    public ActivityLogsController(IActivityLogService service, ILogger<ActivityLogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get paged activity logs with filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ActivityLogFilterDto filter)
    {
        try
        {
            var (items, total) = await _service.GetPagedAsync(filter);
            return Ok(new
            {
                success = true,
                data = items,
                total,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling((double)total / filter.PageSize),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity logs");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get recent activity logs (last N entries)
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 100)
    {
        try
        {
            var logs = await _service.GetRecentAsync(Math.Clamp(limit, 1, 500));
            return Ok(ApiResponse<List<ActivityLogDto>>.SuccessResponse(logs, "Recent logs retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent activity logs");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get activity logs for a specific publish job
    /// </summary>
    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetByJobId(string jobId)
    {
        try
        {
            var logs = await _service.GetByJobIdAsync(jobId);
            return Ok(ApiResponse<List<ActivityLogDto>>.SuccessResponse(logs, "Job logs retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity logs for job {JobId}", jobId);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get activity log statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _service.GetStatsAsync();
            return Ok(ApiResponse<ActivityLogStatsDto>.SuccessResponse(stats, "Stats retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity log stats");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get a single activity log by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var log = await _service.GetByIdAsync(id);
            if (log == null)
                return NotFound(ApiResponse.ErrorResponse($"Activity log '{id}' not found"));
            return Ok(ApiResponse<ActivityLogDto>.SuccessResponse(log));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity log {Id}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Create a new activity log entry
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityLogDto dto)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            var created = await _service.CreateAsync(dto, ipAddress, userAgent);
            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResponse<ActivityLogDto>.SuccessResponse(created, "Activity log created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating activity log");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Bulk create activity log entries
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] List<CreateActivityLogDto> dtos)
    {
        try
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest(ApiResponse.ErrorResponse("No log entries provided"));

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var tasks = dtos.Select(dto => _service.CreateAsync(dto, ipAddress, userAgent));
            var results = await Task.WhenAll(tasks);

            return Ok(new { success = true, created = results.Length });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating activity logs");
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
