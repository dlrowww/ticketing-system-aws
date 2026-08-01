using System.Net;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Validators.Categories;

namespace TicketingSystem.Api.Services.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly ICategoryValidator _validator;

    public CategoryService(AppDbContext db, ICategoryValidator validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken ct)
    {
        var query = _db.Categories.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query
            .OrderBy(c => c.CategoryId)
            .Select(c => new CategoryDto(
                c.CategoryId,
                c.NamePl,
                c.NameEn,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .ToListAsync(ct);

        return categories;
    }

    public async Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken ct)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new CategoryDto(
                c.CategoryId,
                c.NamePl,
                c.NameEn,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest req, CancellationToken ct)
    {
        var normalized = _validator.ValidateAndNormalize(req);

        // Check for duplicate names
        var duplicatePl = await _db.Categories.AnyAsync(c => c.NamePl.ToLower() == normalized.NamePl.ToLower(), ct);
        if (duplicatePl)
        {
            throw new AppException(ErrorCodes.CategoryNameAlreadyExists, "Polish name already exists", HttpStatusCode.Conflict);
        }

        var duplicateEn = await _db.Categories.AnyAsync(c => c.NameEn.ToLower() == normalized.NameEn.ToLower(), ct);
        if (duplicateEn)
        {
            throw new AppException(ErrorCodes.CategoryNameAlreadyExists, "English name already exists", HttpStatusCode.Conflict);
        }

        var category = new Category
        {
            NamePl = normalized.NamePl,
            NameEn = normalized.NameEn,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);

        return new CategoryDto(
            category.CategoryId,
            category.NamePl,
            category.NameEn,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        );
    }

    public async Task<CategoryDto> UpdateAsync(int categoryId, UpdateCategoryRequest req, CancellationToken ct)
    {
        var normalized = _validator.ValidateAndNormalize(req);

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null)
        {
            throw new AppException(ErrorCodes.CategoryNotFound, "Category not found", HttpStatusCode.NotFound);
        }

        // Check for duplicate names (excluding current category)
        if (normalized.NamePl is not null)
        {
            var duplicatePl = await _db.Categories.AnyAsync(
                c => c.CategoryId != categoryId && c.NamePl.ToLower() == normalized.NamePl.ToLower(), ct);
            if (duplicatePl)
            {
                throw new AppException(ErrorCodes.CategoryNameAlreadyExists, "Polish name already exists", HttpStatusCode.Conflict);
            }
            category.NamePl = normalized.NamePl;
        }

        if (normalized.NameEn is not null)
        {
            var duplicateEn = await _db.Categories.AnyAsync(
                c => c.CategoryId != categoryId && c.NameEn.ToLower() == normalized.NameEn.ToLower(), ct);
            if (duplicateEn)
            {
                throw new AppException(ErrorCodes.CategoryNameAlreadyExists, "English name already exists", HttpStatusCode.Conflict);
            }
            category.NameEn = normalized.NameEn;
        }

        if (normalized.IsActive is not null)
        {
            // If deactivating (setting to false), check if category is in use
            if (normalized.IsActive.Value == false && category.IsActive == true)
            {
                var hasTickets = await _db.Tickets.AnyAsync(t => t.CategoryId == categoryId, ct);
                if (hasTickets)
                {
                    throw new AppException(ErrorCodes.CategoryInUse, 
                        "Cannot deactivate category - it is in use by tickets", 
                        HttpStatusCode.Conflict);
                }

                var hasUsers = await _db.Users.AnyAsync(u => u.CategoryId == categoryId, ct);
                if (hasUsers)
                {
                    throw new AppException(ErrorCodes.CategoryInUse, 
                        "Cannot deactivate category - it is in use by users", 
                        HttpStatusCode.Conflict);
                }
            }
            
            category.IsActive = normalized.IsActive.Value;
        }

        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new CategoryDto(
            category.CategoryId,
            category.NamePl,
            category.NameEn,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt
        );
    }

    public async Task DeleteAsync(int categoryId, CancellationToken ct)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null)
        {
            throw new AppException(ErrorCodes.CategoryNotFound, "Category not found", HttpStatusCode.NotFound);
        }

        // Check if category is in use by tickets or users
        var hasTickets = await _db.Tickets.AnyAsync(t => t.CategoryId == categoryId, ct);
        if (hasTickets)
        {
            throw new AppException(ErrorCodes.CategoryInUse, "Category is in use by tickets and cannot be deleted", HttpStatusCode.Conflict);
        }

        var hasUsers = await _db.Users.AnyAsync(u => u.CategoryId == categoryId, ct);
        if (hasUsers)
        {
            throw new AppException(ErrorCodes.CategoryInUse, "Category is in use by users and cannot be deleted", HttpStatusCode.Conflict);
        }

        // Hard delete - physically remove from database
        // Safe because we've verified the category is not in use
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }
}
