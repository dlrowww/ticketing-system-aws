namespace TicketingSystem.Api.DTOs.Reports;

public sealed record TicketCountByPriorityDto(byte Priority, int Count);
