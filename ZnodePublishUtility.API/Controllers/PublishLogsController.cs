using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;
using ZnodePublishUtility.Service.Interfaces;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/publish-logs")]
public class PublishLogsController : ControllerBase
{
    private readonly IActivityLogService _service;
    private readonly ILogger<PublishLogsController> _logger;

    public PublishLogsController(IActivityLogService service, ILogger<PublishLogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all publish logs for a job
    /// </summary>
    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetByJobId(string jobId)
    {
        try
        {
            var logs = await _service.GetPublishLogsByJobIdAsync(jobId);
            return Ok(ApiResponse<List<PublishLogDto>>.SuccessResponse(logs, "Publish logs retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish logs for job {JobId}", jobId);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Create a single publish log entry
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePublishLogDto dto)
    {
        try
        {
            var created = await _service.CreatePublishLogAsync(dto);
            return Ok(ApiResponse<PublishLogDto>.SuccessResponse(created, "Publish log created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating publish log for job {JobId}", dto.JobId);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Bulk create publish log entries for a job (batch persistence from frontend)
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreatePublishLogDto dto)
    {
        try
        {
            if (dto == null || dto.Logs.Count == 0)
                return BadRequest(ApiResponse.ErrorResponse("No log entries provided"));

            await _service.BulkCreatePublishLogsAsync(dto);
            return Ok(new { success = true, created = dto.Logs.Count, jobId = dto.JobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating publish logs for job {JobId}", dto?.JobId);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
