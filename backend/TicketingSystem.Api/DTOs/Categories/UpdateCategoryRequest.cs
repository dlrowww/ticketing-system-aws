namespace TicketingSystem.Api.DTOs.Categories;

/// <summary>
/// Request to update an existing category
/// </summary>
public sealed record UpdateCategoryRequest
{
    public string? NamePl { get; init; }
    public string? NameEn { get; init; }
    public bool? IsActive { get; init; }
}
