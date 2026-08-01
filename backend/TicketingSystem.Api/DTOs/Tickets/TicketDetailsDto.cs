namespace TicketingSystem.Api.DTOs.Tickets
{
    public record class TicketDetailsDto
    {
        public int TicketId { get; init; }
        public string Title { get; init; } = default!;
        public string? Description { get; init; }
        public int CategoryId { get; init; }
        public int Priority { get; init; }
        public int Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public int CreatedById { get; init; }
        public int? AssignedToId { get; init; }
        public string? CreatedByName { get; init; }
        public string? AssignedToName { get; init; }
        
        /// <summary>
        /// Edit capabilities for the current user viewing this ticket.
        /// </summary>
        public TicketEditCapabilities? Capabilities { get; init; }
    }
}