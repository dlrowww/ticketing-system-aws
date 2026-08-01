namespace TicketingSystem.Api.DTOs.History;

public record TicketHistoryDto(
    int HistoryId,
    int TicketId,
    string ChangeType,
    string? OldValue,
    string? NewValue,
    string? OldValueDisplay,
    string? NewValueDisplay,
    string ChangedByName,
    DateTime ChangedAt
);
