namespace TicketingSystem.Api.Services.Email;

/// <summary>
/// Service for sending email notifications for ticket lifecycle events.
/// All methods use fire-and-forget pattern - email failures do not block ticket operations.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends email notification when a new ticket is auto-assigned.
    /// Sent to: Assignee only.
    /// </summary>
    /// <param name="ticketId">ID of the assigned ticket</param>
    /// <param name="assigneeId">ID of the user assigned to the ticket</param>
    /// <param name="ct">Cancellation token</param>
    Task SendTicketAssignedAsync(int ticketId, int assigneeId, CancellationToken ct = default);

    /// <summary>
    /// Sends email notification when a ticket is reassigned to a different user.
    /// Sent to: New assignee + old assignee (if existed).
    /// </summary>
    /// <param name="ticketId">ID of the reassigned ticket</param>
    /// <param name="oldAssigneeId">ID of the previous assignee (null if none)</param>
    /// <param name="newAssigneeId">ID of the new assignee</param>
    /// <param name="reassignedBy">ID of the user who performed the reassignment</param>
    /// <param name="ct">Cancellation token</param>
    Task SendTicketReassignedAsync(int ticketId, int? oldAssigneeId, int newAssigneeId, int reassignedBy, CancellationToken ct = default);

    /// <summary>
    /// Sends email notification when ticket status changes (except Resolved - use SendTicketResolvedAsync).
    /// Sent to: Creator + Assignee (deduplicated).
    /// </summary>
    /// <param name="ticketId">ID of the ticket</param>
    /// <param name="oldStatus">Previous status value</param>
    /// <param name="newStatus">New status value</param>
    /// <param name="changedBy">ID of the user who changed the status</param>
    /// <param name="ct">Cancellation token</param>
    Task SendTicketStatusChangedAsync(int ticketId, byte oldStatus, byte newStatus, int changedBy, CancellationToken ct = default);

    /// <summary>
    /// Sends email notification when a comment is added to a ticket.
    /// Sent to: Creator + Assignee (excluding the commenter, deduplicated).
    /// </summary>
    /// <param name="ticketId">ID of the ticket</param>
    /// <param name="commentId">ID of the new comment</param>
    /// <param name="commenterId">ID of the user who added the comment</param>
    /// <param name="ct">Cancellation token</param>
    Task SendCommentAddedAsync(int ticketId, int commentId, int commenterId, CancellationToken ct = default);

    /// <summary>
    /// Sends email notification when ticket priority is escalated to High or Critical.
    /// Sent to: Creator + Assignee (deduplicated).
    /// </summary>
    /// <param name="ticketId">ID of the ticket</param>
    /// <param name="oldPriority">Previous priority value</param>
    /// <param name="newPriority">New priority value (High or Critical)</param>
    /// <param name="changedBy">ID of the user who escalated the priority</param>
    /// <param name="ct">Cancellation token</param>
    Task SendPriorityEscalatedAsync(int ticketId, byte oldPriority, byte newPriority, int changedBy, CancellationToken ct = default);

    /// <summary>
    /// Sends email notification when a ticket is resolved.
    /// Sent to: Creator only.
    /// </summary>
    /// <param name="ticketId">ID of the resolved ticket</param>
    /// <param name="resolvedBy">ID of the user who resolved the ticket</param>
    /// <param name="ct">Cancellation token</param>
    Task SendTicketResolvedAsync(int ticketId, int resolvedBy, CancellationToken ct = default);
}
