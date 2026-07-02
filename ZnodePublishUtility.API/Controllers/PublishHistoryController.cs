using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublishHistoryController : ControllerBase
{
    private readonly IPublishHistoryService _publishHistoryService;
    private readonly ILogger<PublishHistoryController> _logger;

    public PublishHistoryController(IPublishHistoryService publishHistoryService, ILogger<PublishHistoryController> logger)
    {
        _publishHistoryService = publishHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all publish history
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PublishHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all publish history");
            var history = await _publishHistoryService.GetAllHistoryAsync();
            return Ok(ApiResponse<List<PublishHistoryDto>>.SuccessResponse(history, "Publish history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish history");
            return StatusCode(500, ApiResponse<List<PublishHistoryDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish history by type
    /// </summary>
    [HttpGet("by-type/{type}")]
    [ProducesResponseType(typeof(ApiResponse<List<PublishHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByType(string type)
    {
        try
        {
            _logger.LogInformation("Fetching publish history by type: {Type}", type);
            var history = await _publishHistoryService.GetHistoryByTypeAsync(type);
            return Ok(ApiResponse<List<PublishHistoryDto>>.SuccessResponse(history, "Publish history retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid publish type");
            return BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish history by type: {Type}", type);
            return StatusCode(500, ApiResponse<List<PublishHistoryDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish history by status
    /// </summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<PublishHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        try
        {
            _logger.LogInformation("Fetching publish history by status: {Status}", status);
            var history = await _publishHistoryService.GetHistoryByStatusAsync(status);
            return Ok(ApiResponse<List<PublishHistoryDto>>.SuccessResponse(history, "Publish history retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid publish status");
            return BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish history by status: {Status}", status);
            return StatusCode(500, ApiResponse<List<PublishHistoryDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish history by target name
    /// </summary>
    [HttpGet("by-target/{targetName}")]
    [ProducesResponseType(typeof(ApiResponse<List<PublishHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTargetName(string targetName)
    {
        try
        {
            _logger.LogInformation("Fetching publish history for target: {TargetName}", targetName);
            var history = await _publishHistoryService.GetHistoryByTargetNameAsync(targetName);
            return Ok(ApiResponse<List<PublishHistoryDto>>.SuccessResponse(history, "Publish history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish history for target: {TargetName}", targetName);
            return StatusCode(500, ApiResponse<List<PublishHistoryDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get publish history record by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PublishHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            _logger.LogInformation("Fetching publish history record with ID: {HistoryId}", id);
            var history = await _publishHistoryService.GetHistoryByIdAsync(id);
            
            if (history == null)
                return NotFound(ApiResponse.ErrorResponse($"Publish history record with ID '{id}' not found"));

            return Ok(ApiResponse<PublishHistoryDto>.SuccessResponse(history, "Publish history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching publish history record with ID: {HistoryId}", id);
            return StatusCode(500, ApiResponse<PublishHistoryDto>.ErrorResponse("Internal server error"));
        }
    }
}
