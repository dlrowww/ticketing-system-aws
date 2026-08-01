namespace TicketingSystem.Api.DTOs.Tickets
{
    public record TicketListItemDto(
        int TicketId,
        string Title,
        int CategoryId,
        byte Priority,   // TicketPriority numeric enum value
        byte Status,     // TicketStatus numeric enum value
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CreatedByName,
        string? AssignedToName
    );
}