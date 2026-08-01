namespace TicketingSystem.Api.DTOs.Categories;

/// <summary>
/// Category details response
/// </summary>
public sealed record CategoryDto(
    int CategoryId,
    string NamePl,
    string NameEn,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
