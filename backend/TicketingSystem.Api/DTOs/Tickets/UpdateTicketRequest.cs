using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.DTOs.Tickets
{
    /// <summary>
    /// Partial update for a ticket. Only non-null fields will be applied.
    /// </summary>
    public sealed class UpdateTicketRequest
    {
        // Text fields (optional)
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Enums (optional)
        public int? CategoryId { get; set; }
        public TicketPriority? Priority { get; set; }
        public TicketStatus? Status { get; set; }

        /// <summary>
        /// Assign ticket to a specific user (optional).
        /// </summary>
        public int? AssignedToUserId { get; set; }

        /// <summary>
        /// If true, clears the assignment (sets AssignedToId = null).
        /// Cannot be combined with AssignedToUserId.
        /// </summary>
        public bool? ClearAssignment { get; set; }
    }
}