using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Services.Categories;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    /// <summary>
    /// Get all categories. By default, only active categories are returned.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var categories = await _categories.GetAllAsync(includeInactive, ct);
        return Ok(categories);
    }

    /// <summary>
    /// Get a single category by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById([FromRoute] int id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
        {
            return NotFound();
        }
        return Ok(category);
    }

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest req,
        CancellationToken ct = default)
    {
        var category = await _categories.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
    }

    /// <summary>
    /// Update an existing category (Admin only)
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateCategoryRequest req,
        CancellationToken ct = default)
    {
        var category = await _categories.UpdateAsync(id, req, ct);
        return Ok(category);
    }

    /// <summary>
    /// Delete a category (Admin only). Physically removes from database.
    /// Cannot delete if category is in use by tickets or users (returns 409 Conflict).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
    {
        await _categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
