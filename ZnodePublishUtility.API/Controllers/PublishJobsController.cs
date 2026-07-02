using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublishJobsController : ControllerBase
{
    private readonly IPublishJobService _publishJobService;
    private readonly ILogger<PublishJobsController> _logger;
    private const string UserId = "api-user";

    public PublishJobsController(IPublishJobService publishJobService, ILogger<PublishJobsController> logger)
    {
        _publishJobService = publishJobService;
        _logger = logger;
    }

    /// <summary>
    /// Get all publish jobs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PublishJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all publish jobs");
            var jobs = await _publishJobService.GetAllJobsAsync();
            return Ok(ApiResponse<List<PublishJobDto>>.SuccessResponse(jobs, "Publish jobs retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish jobs");
            return StatusCode(500, ApiResponse<List<PublishJobDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish job by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PublishJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            _logger.LogInformation("Fetching publish job with ID: {JobId}", id);
            var job = await _publishJobService.GetJobByIdAsync(id);
            
            if (job == null)
                return NotFound(ApiResponse.ErrorResponse($"Publish job with ID '{id}' not found"));

            return Ok(ApiResponse<PublishJobDto>.SuccessResponse(job, "Publish job retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish job with ID: {JobId}", id);
            return StatusCode(500, ApiResponse<PublishJobDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish job logs
    /// </summary>
    [HttpGet("{jobId}/logs")]
    [ProducesResponseType(typeof(ApiResponse<List<PublishLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(string jobId)
    {
        try
        {
            _logger.LogInformation("Fetching logs for publish job: {JobId}", jobId);
            var logs = await _publishJobService.GetJobLogsAsync(jobId);
            return Ok(ApiResponse<List<PublishLogDto>>.SuccessResponse(logs, "Publish job logs retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching logs for publish job: {JobId}", jobId);
            return StatusCode(500, ApiResponse<List<PublishLogDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish job progress
    /// </summary>
    [HttpGet("{jobId}/progress")]
    [ProducesResponseType(typeof(ApiResponse<PublishProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgress(string jobId)
    {
        try
        {
            _logger.LogInformation("Fetching progress for publish job: {JobId}", jobId);
            var progress = await _publishJobService.GetJobProgressAsync(jobId);
            
            if (progress == null)
                return NotFound(ApiResponse.ErrorResponse($"Progress for publish job '{jobId}' not found"));

            return Ok(ApiResponse<PublishProgressDto>.SuccessResponse(progress, "Publish job progress retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching progress for publish job: {JobId}", jobId);
            return StatusCode(500, ApiResponse<PublishProgressDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Start a new publish job
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<PublishJobDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start([FromBody] StartPublishDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Starting new {PublishType} publish job for target: {TargetId}", dto.Type, dto.TargetId);
            var job = await _publishJobService.StartPublishAsync(dto, UserId);

            return CreatedAtAction(nameof(GetById), new { id = job.Id }, 
                ApiResponse<PublishJobDto>.SuccessResponse(job, "Publish job started successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when starting publish job");
            return BadRequest(ApiResponse<PublishJobDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting publish job");
            return StatusCode(500, ApiResponse<PublishJobDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Cancel a publish job
    /// </summary>
    [HttpPost("{jobId}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(string jobId)
    {
        try
        {
            _logger.LogInformation("Cancelling publish job: {JobId}", jobId);
            var result = await _publishJobService.CancelPublishAsync(jobId);

            if (!result)
                return NotFound(ApiResponse.ErrorResponse($"Cannot cancel publish job '{jobId}' - job not found or already completed"));

            return Ok(ApiResponse.SuccessResponse("Publish job cancelled successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling publish job: {JobId}", jobId);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
