namespace TicketingSystem.Api.DTOs.Categories;

/// <summary>
/// Request to create a new category
/// </summary>
public sealed record CreateCategoryRequest
{
    public string? NamePl { get; init; }
    public string? NameEn { get; init; }
}
