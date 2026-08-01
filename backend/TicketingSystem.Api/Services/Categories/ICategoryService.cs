using TicketingSystem.Api.DTOs.Categories;

namespace TicketingSystem.Api.Services.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct);
    Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken ct);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest req, CancellationToken ct);
    Task<CategoryDto> UpdateAsync(int categoryId, UpdateCategoryRequest req, CancellationToken ct);
    Task DeleteAsync(int categoryId, CancellationToken ct);
}
