using TicketingSystem.Api.DTOs.Reports;

namespace TicketingSystem.Api.Services.Reporting;

public interface IReportingService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct);
    Task<IReadOnlyList<TicketCountByCategoryDto>> GetTicketsByCategoryAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct);
    Task<IReadOnlyList<TicketCountByStatusDto>> GetTicketsByStatusAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct);
    Task<IReadOnlyList<TicketCountByPriorityDto>> GetTicketsByPriorityAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct);
    Task<IReadOnlyList<TicketTrendDto>> GetTicketTrendAsync(DateOnly? from, DateOnly? to, int days, byte? category, CancellationToken ct);
}
