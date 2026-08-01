using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.DTOs.Tickets
{
    public sealed record CreateTicketResponse(
        int TicketId,
        TicketStatus Status,
        int? AssignedToUserId,
        DateTime CreatedAt
    );
}