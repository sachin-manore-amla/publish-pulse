using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortalsController : ControllerBase
{
    private readonly IPortalService _portalService;
    private readonly ILogger<PortalsController> _logger;
    private const string UserId = "api-user";

    public PortalsController(IPortalService portalService, ILogger<PortalsController> logger)
    {
        _portalService = portalService;
        _logger = logger;
    }

    /// <summary>
    /// Get all portals
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PortalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all portals");
            var portals = await _portalService.GetAllPortalsAsync();
            return Ok(ApiResponse<List<PortalDto>>.SuccessResponse(portals, "Portals retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portals");
            return StatusCode(500, ApiResponse<List<PortalDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get portals by store ID
    /// </summary>
    [HttpGet("by-store/{storeId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PortalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStoreId(string storeId)
    {
        try
        {
            _logger.LogInformation("Fetching portals for store: {StoreId}", storeId);
            var portals = await _portalService.GetPortalsByStoreIdAsync(storeId);
            return Ok(ApiResponse<List<PortalDto>>.SuccessResponse(portals, "Portals retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portals for store: {StoreId}", storeId);
            return StatusCode(500, ApiResponse<List<PortalDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get portal by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PortalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            _logger.LogInformation("Fetching portal with ID: {PortalId}", id);
            var portal = await _portalService.GetPortalByIdAsync(id);
            
            if (portal == null)
                return NotFound(ApiResponse.ErrorResponse($"Portal with ID '{id}' not found"));

            return Ok(ApiResponse<PortalDto>.SuccessResponse(portal, "Portal retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portal with ID: {PortalId}", id);
            return StatusCode(500, ApiResponse<PortalDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Create a new portal
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PortalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePortalDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Creating new portal: {PortalName}", dto.Name);
            var portal = await _portalService.CreatePortalAsync(dto, UserId);

            return CreatedAtAction(nameof(GetById), new { id = portal.Id }, 
                ApiResponse<PortalDto>.SuccessResponse(portal, "Portal created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portal");
            return StatusCode(500, ApiResponse<PortalDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Update a portal
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PortalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePortalDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Updating portal with ID: {PortalId}", id);
            var portal = await _portalService.UpdatePortalAsync(id, dto);

            if (portal == null)
                return NotFound(ApiResponse.ErrorResponse($"Portal with ID '{id}' not found"));

            return Ok(ApiResponse<PortalDto>.SuccessResponse(portal, "Portal updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portal with ID: {PortalId}", id);
            return StatusCode(500, ApiResponse<PortalDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Delete a portal
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            _logger.LogInformation("Deleting portal with ID: {PortalId}", id);
            var result = await _portalService.DeletePortalAsync(id);

            if (!result)
                return NotFound(ApiResponse.ErrorResponse($"Portal with ID '{id}' not found"));

            return Ok(ApiResponse.SuccessResponse("Portal deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting portal with ID: {PortalId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
