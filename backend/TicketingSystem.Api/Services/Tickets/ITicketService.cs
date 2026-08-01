using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.DTOs.Users;

namespace TicketingSystem.Api.Services;

public interface ITicketService
{
    /// <summary>
    /// Creates a new ticket (and optional attachments), applies auto-assignment rules,
    /// writes a "Created" history entry, and returns a minimal creation payload.
    /// </summary>
    Task<CreateTicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct);

    /// <summary>
    /// Updates the ticket with either title, description, priority, status, category and or assignment.
    /// </summary>
    Task<TicketDetailsDto> UpdateAsync(int id, UpdateTicketRequest req, CancellationToken ct);

    /// <summary>
    /// Hard delete - avaialble ONLY for research.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    /// <summary>
    /// Get a ticket by Id.
    /// </summary>
    Task<TicketDetailsDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Get list of tickets by query.
    /// </summary>
    Task<Paged<TicketListItemDto>> GetListAsync(TicketListQuery q, CancellationToken ct);

    /// <summary>
    /// Get list of tickets by query for export (without pagination)
    /// </summary>
    Task<IReadOnlyList<TicketListItemDto>> ExportAsync(TicketExportQuery q, CancellationToken ct);

    /// <summary>
    /// Gets users who can be assigned to this ticket.
    /// Filters by role (Support/TeamLeader/Admin) and category.
    /// </summary>
    Task<IReadOnlyList<AssignableUserDto>> GetAssignableUsersAsync(int ticketId, CancellationToken ct);
}
