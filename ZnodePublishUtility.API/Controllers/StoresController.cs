using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly ILogger<StoresController> _logger;
    private const string UserId = "api-user";

    public StoresController(IStoreService storeService, ILogger<StoresController> logger)
    {
        _storeService = storeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all stores
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoreDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all stores");
            var stores = await _storeService.GetAllStoresAsync();
            return Ok(ApiResponse<List<StoreDto>>.SuccessResponse(stores, "Stores retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stores");
            return StatusCode(500, ApiResponse<List<StoreDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get store by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            _logger.LogInformation("Fetching store with ID: {StoreId}", id);
            var store = await _storeService.GetStoreByIdAsync(id);
            
            if (store == null)
                return NotFound(ApiResponse.ErrorResponse($"Store with ID '{id}' not found"));

            return Ok(ApiResponse<StoreDto>.SuccessResponse(store, "Store retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching store with ID: {StoreId}", id);
            return StatusCode(500, ApiResponse<StoreDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Create a new store
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Creating new store: {StoreName}", dto.Name);
            var store = await _storeService.CreateStoreAsync(dto, UserId);

            return CreatedAtAction(nameof(GetById), new { id = store.Id }, 
                ApiResponse<StoreDto>.SuccessResponse(store, "Store created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store");
            return StatusCode(500, ApiResponse<StoreDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Update a store
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateStoreDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Updating store with ID: {StoreId}", id);
            var store = await _storeService.UpdateStoreAsync(id, dto);

            if (store == null)
                return NotFound(ApiResponse.ErrorResponse($"Store with ID '{id}' not found"));

            return Ok(ApiResponse<StoreDto>.SuccessResponse(store, "Store updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store with ID: {StoreId}", id);
            return StatusCode(500, ApiResponse<StoreDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Delete a store
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            _logger.LogInformation("Deleting store with ID: {StoreId}", id);
            var result = await _storeService.DeleteStoreAsync(id);

            if (!result)
                return NotFound(ApiResponse.ErrorResponse($"Store with ID '{id}' not found"));

            return Ok(ApiResponse.SuccessResponse("Store deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store with ID: {StoreId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
