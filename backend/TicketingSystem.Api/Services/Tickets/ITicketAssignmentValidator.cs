namespace TicketingSystem.Api.Services.Tickets;

/// <summary>
/// Validates ticket assignment operations.
/// </summary>
public interface ITicketAssignmentValidator
{
    /// <summary>
    /// Validates that a user can be assigned to a ticket.
    /// Checks: user exists, is active, has eligible role (Support/TeamLeader/Admin),
    /// and category matches for Support/TeamLeader (Admin exempt).
    /// </summary>
    /// <param name="ticketCategoryId">The category ID of the ticket being assigned.</param>
    /// <param name="userId">The user ID to validate for assignment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="AppException">Thrown if validation fails with appropriate error code.</exception>
    Task ValidateAssignmentAsync(int ticketCategoryId, int userId, CancellationToken ct);
}
