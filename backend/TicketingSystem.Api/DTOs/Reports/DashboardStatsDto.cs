namespace TicketingSystem.Api.DTOs.Reports;

public sealed record DashboardStatsDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    double AverageResolutionTimeHours
);
