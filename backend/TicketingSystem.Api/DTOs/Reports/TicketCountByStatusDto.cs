namespace TicketingSystem.Api.DTOs.Reports;

public sealed record TicketCountByStatusDto(byte Status, int Count);
