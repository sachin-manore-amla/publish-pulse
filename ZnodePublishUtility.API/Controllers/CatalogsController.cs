using Microsoft.AspNetCore.Mvc;
using ZnodePublishUtility.Service.Interfaces;
using ZnodePublishUtility.Models.DTOs;
using ZnodePublishUtility.Models.Responses;

namespace ZnodePublishUtility.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogsController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ICatalogProductCountService _catalogProductCountService;
    private readonly ILogger<CatalogsController> _logger;
    private const string UserId = "api-user"; // In real app, would get from claims

    public CatalogsController(
        ICatalogService catalogService,
        ICatalogProductCountService catalogProductCountService,
        ILogger<CatalogsController> logger)
    {
        _catalogService = catalogService;
        _catalogProductCountService = catalogProductCountService;
        _logger = logger;
    }

    /// <summary>
    /// Draft (not-yet-published) product counts per catalog, sourced directly from SQL Server
    /// via <c>EXEC dbo.GetCatalogProductCounts;</c> — not the published-product count on the
    /// Catalog entity returned by <see cref="GetAll"/>.
    /// </summary>
    [HttpGet("product-counts")]
    [ProducesResponseType(typeof(ApiResponse<List<CatalogProductCountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductCounts()
    {
        try
        {
            _logger.LogInformation("Fetching catalog draft product counts from SQL Server");
            var counts = await _catalogProductCountService.GetCatalogProductCountsAsync();
            return Ok(ApiResponse<List<CatalogProductCountDto>>.SuccessResponse(counts, "Catalog product counts retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching catalog product counts");
            return StatusCode(500, ApiResponse<List<CatalogProductCountDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get all catalogs
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CatalogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            _logger.LogInformation("Fetching all catalogs");
            var catalogs = await _catalogService.GetAllCatalogsAsync();
            return Ok(ApiResponse<List<CatalogDto>>.SuccessResponse(catalogs, "Catalogs retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching catalogs");
            return StatusCode(500, ApiResponse<List<CatalogDto>>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Get catalog by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            _logger.LogInformation("Fetching catalog with ID: {CatalogId}", id);
            var catalog = await _catalogService.GetCatalogByIdAsync(id);
            
            if (catalog == null)
                return NotFound(ApiResponse.ErrorResponse($"Catalog with ID '{id}' not found"));

            return Ok(ApiResponse<CatalogDto>.SuccessResponse(catalog, "Catalog retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching catalog with ID: {CatalogId}", id);
            return StatusCode(500, ApiResponse<CatalogDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Create a new catalog
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CatalogDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCatalogDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Creating new catalog: {CatalogName}", dto.Name);
            var catalog = await _catalogService.CreateCatalogAsync(dto, UserId);

            return CreatedAtAction(nameof(GetById), new { id = catalog.Id }, 
                ApiResponse<CatalogDto>.SuccessResponse(catalog, "Catalog created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating catalog");
            return StatusCode(500, ApiResponse<CatalogDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Update a catalog
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CatalogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCatalogDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Invalid input"));

            _logger.LogInformation("Updating catalog with ID: {CatalogId}", id);
            var catalog = await _catalogService.UpdateCatalogAsync(id, dto);

            if (catalog == null)
                return NotFound(ApiResponse.ErrorResponse($"Catalog with ID '{id}' not found"));

            return Ok(ApiResponse<CatalogDto>.SuccessResponse(catalog, "Catalog updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating catalog with ID: {CatalogId}", id);
            return StatusCode(500, ApiResponse<CatalogDto>.ErrorResponse("Internal server error"));
        }
    }

    /// <summary>
    /// Delete a catalog
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            _logger.LogInformation("Deleting catalog with ID: {CatalogId}", id);
            var result = await _catalogService.DeleteCatalogAsync(id);

            if (!result)
                return NotFound(ApiResponse.ErrorResponse($"Catalog with ID '{id}' not found"));

            return Ok(ApiResponse.SuccessResponse("Catalog deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting catalog with ID: {CatalogId}", id);
            return StatusCode(500, ApiResponse.ErrorResponse("Internal server error"));
        }
    }
}
