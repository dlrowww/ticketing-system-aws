using TicketingSystem.Api.DTOs.History;
using TicketingSystem.Api.Enums.History;

namespace TicketingSystem.Api.Services;

public interface ITicketHistoryService
{
    /// <summary>
    /// Logs a change to a ticket in the history table.
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="changeType">Type of change (enum value)</param>
    /// <param name="oldValue">Previous value (null for creation events)</param>
    /// <param name="newValue">New value</param>
    /// <param name="changedBy">User ID who made the change</param>
    /// <param name="ct">Cancellation token</param>
    Task LogChangeAsync(
        int ticketId,
        HistoryChangeType changeType,
        string? oldValue,
        string? newValue,
        int changedBy,
        CancellationToken ct);

    /// <summary>
    /// Retrieves the complete history of changes for a ticket, ordered chronologically (oldest first).
    /// </summary>
    /// <param name="ticketId">The ticket ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of history entries</returns>
    Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(int ticketId, CancellationToken ct);
}
