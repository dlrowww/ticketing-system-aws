namespace TicketingSystem.Api.DTOs.Lookups;

public record LookupsResponse(
    IReadOnlyList<LookupItem> TicketStatus,
    IReadOnlyList<LookupItem> Priority,
    IReadOnlyList<LookupItem> Category,
    IReadOnlyList<LookupItem> UserRole,
    IReadOnlyList<LookupItem> HistoryChangeType,
    string Version
);