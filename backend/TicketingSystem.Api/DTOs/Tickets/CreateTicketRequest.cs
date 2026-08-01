using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.DTOs.Tickets
{    
    /// <summary>
    /// Request payload to create a new ticket.
    /// </summary>
    public class CreateTicketRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public TicketPriority? Priority { get; set; }
        public List<IFormFile>? Files { get; set; }
    }
}
