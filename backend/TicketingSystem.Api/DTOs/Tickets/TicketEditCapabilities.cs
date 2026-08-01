namespace TicketingSystem.Api.DTOs.Tickets;

/// <summary>
/// Describes what edit operations the current user can perform on a ticket.
/// </summary>
public class TicketEditCapabilities
{
    /// <summary>
    /// Whether the user can edit the ticket at all.
    /// </summary>
    public bool CanEdit { get; init; }

    /// <summary>
    /// Whether the user can edit the ticket title.
    /// </summary>
    public bool CanEditTitle { get; init; }

    /// <summary>
    /// Whether the user can edit the ticket description.
    /// </summary>
    public bool CanEditDescription { get; init; }

    /// <summary>
    /// Whether the user can change the ticket category.
    /// </summary>
    public bool CanEditCategory { get; init; }

    /// <summary>
    /// Whether the user can change the ticket priority.
    /// </summary>
    public bool CanEditPriority { get; init; }

    /// <summary>
    /// Whether the user can change the ticket status.
    /// </summary>
    public bool CanEditStatus { get; init; }

    /// <summary>
    /// Whether the user can reassign the ticket.
    /// </summary>
    public bool CanEditAssignment { get; init; }
}
