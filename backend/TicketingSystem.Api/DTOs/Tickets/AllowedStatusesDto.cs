namespace TicketingSystem.Api.DTOs.Tickets;

/// <summary>
/// Response DTO containing list of allowed status transitions for a ticket.
/// </summary>
public record AllowedStatusesDto
{
    /// <summary>
    /// List of status values (byte) that are valid transitions from the ticket's current status.
    /// Always includes the current status itself.
    /// </summary>
    public required IReadOnlyList<byte> AllowedStatuses { get; init; }
}
