using Microsoft.EntityFrameworkCore;

using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Reports;
using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.Services.Reporting;

public sealed class ReportingService : IReportingService
{
    private readonly AppDbContext _db;

    public ReportingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct)
    {
        var query = ApplyFilters(_db.Tickets.AsNoTracking(), from, to, category);

        var total = await query.CountAsync(ct);

        // Combine New + Open (tickets awaiting action)
        var open = await query.CountAsync(t => t.Status == TicketStatus.New || t.Status == TicketStatus.Open, ct);
        var inProgress = await query.CountAsync(t => t.Status == TicketStatus.InProcess, ct);
        var resolved = await query.CountAsync(t => t.Status == TicketStatus.Resolved, ct);

        var avgHours = await GetAverageResolutionTimeHoursAsync(query, ct);

        return new DashboardStatsDto(
            TotalTickets: total,
            OpenTickets: open,
            InProgressTickets: inProgress,
            ResolvedTickets: resolved,
            AverageResolutionTimeHours: avgHours
        );
    }

    public async Task<IReadOnlyList<TicketCountByCategoryDto>> GetTicketsByCategoryAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct)
    {
        var query = ApplyFilters(_db.Tickets.AsNoTracking(), from, to, category);

        // Filter out closed tickets - Category chart shows current workload distribution
        query = query.Where(t => 
            t.Status != TicketStatus.Resolved && 
            t.Status != TicketStatus.Cancelled);

        // EF InMemory can't translate some GroupBy projections; use client grouping there.
        if ((_db.Database.ProviderName ?? string.Empty).Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var categories = await query.Select(t => t.CategoryId).ToListAsync(ct);
            return categories
                .GroupBy(x => x)
                .Select(g => new TicketCountByCategoryDto(g.Key, g.Count()))
                .OrderBy(x => x.CategoryId)
                .ToList();
        }

        var grouped = await query
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return grouped
            .Select(x => new TicketCountByCategoryDto(x.CategoryId, x.Count))
            .OrderBy(x => x.CategoryId)
            .ToList();
    }

    public async Task<IReadOnlyList<TicketCountByStatusDto>> GetTicketsByStatusAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct)
    {
        var query = ApplyFilters(_db.Tickets.AsNoTracking(), from, to, category);

        List<(TicketStatus Status, int Count)> grouped;

        if ((_db.Database.ProviderName ?? string.Empty).Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var statuses = await query.Select(t => t.Status).ToListAsync(ct);
            grouped = statuses
                .GroupBy(x => x)
                .Select(g => (g.Key, g.Count()))
                .ToList();
        }
        else
        {
            var rows = await query
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            grouped = rows
                .Select(x => ((TicketStatus)x.Status, x.Count))
                .ToList();
        }

        var map = grouped.ToDictionary(x => x.Status, x => x.Count);

        static int GetCount(Dictionary<TicketStatus, int> counts, TicketStatus status)
        {
            return counts.TryGetValue(status, out var value) ? value : 0;
        }

        return new List<TicketCountByStatusDto>
        {
            new((byte)TicketStatus.New, GetCount(map, TicketStatus.New)),
            new((byte)TicketStatus.Open, GetCount(map, TicketStatus.Open)),
            new((byte)TicketStatus.InProcess, GetCount(map, TicketStatus.InProcess)),
            new((byte)TicketStatus.Resolved, GetCount(map, TicketStatus.Resolved)),
            new((byte)TicketStatus.Cancelled, GetCount(map, TicketStatus.Cancelled)),
            new((byte)TicketStatus.Postponed, GetCount(map, TicketStatus.Postponed)),
            new((byte)TicketStatus.Returned, GetCount(map, TicketStatus.Returned))
        };
    }

    public async Task<IReadOnlyList<TicketCountByPriorityDto>> GetTicketsByPriorityAsync(DateOnly? from, DateOnly? to, byte? category, CancellationToken ct)
    {
        var query = ApplyFilters(_db.Tickets.AsNoTracking(), from, to, category);

        // Filter out closed tickets - Priority chart shows current workload urgency
        query = query.Where(t => 
            t.Status != TicketStatus.Resolved && 
            t.Status != TicketStatus.Cancelled);

        List<(TicketPriority Priority, int Count)> grouped;

        if ((_db.Database.ProviderName ?? string.Empty).Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var priorities = await query.Select(t => t.Priority).ToListAsync(ct);
            grouped = priorities
                .GroupBy(x => x)
                .Select(g => (g.Key, g.Count()))
                .ToList();
        }
        else
        {
            var rows = await query
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            grouped = rows
                .Select(x => ((TicketPriority)x.Priority, x.Count))
                .ToList();
        }

        var map = grouped.ToDictionary(x => x.Priority, x => x.Count);

        static int GetCount(Dictionary<TicketPriority, int> counts, TicketPriority priority)
        {
            return counts.TryGetValue(priority, out var value) ? value : 0;
        }

        return new List<TicketCountByPriorityDto>
        {
            new((byte)TicketPriority.Low, GetCount(map, TicketPriority.Low)),
            new((byte)TicketPriority.Medium, GetCount(map, TicketPriority.Medium)),
            new((byte)TicketPriority.High, GetCount(map, TicketPriority.High)),
            new((byte)TicketPriority.Critical, GetCount(map, TicketPriority.Critical))
        };
    }

    public async Task<IReadOnlyList<TicketTrendDto>> GetTicketTrendAsync(DateOnly? from, DateOnly? to, int days, byte? category, CancellationToken ct)
    {
        // Determine date range: use explicit dates if both provided, otherwise calculate from days
        DateOnly fromDate;
        DateOnly toDate;
        
        if (from.HasValue || to.HasValue)
        {
            // Use explicit dates (with defaults if only one is provided)
            toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
            fromDate = from ?? toDate.AddDays(-(days > 0 && days <= 365 ? days - 1 : 29));
        }
        else
        {
            // Calculate from days parameter
            if (days <= 0) days = 30;
            if (days > 365) days = 365;
            toDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            fromDate = toDate.AddDays(-(days - 1));
        }

        var query = ApplyFilters(_db.Tickets.AsNoTracking(), fromDate, toDate, category);

        // Group by day (UTC) using DateTime.Date which EF can translate.
        var grouped = await query
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var map = grouped.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);

        // Calculate actual range for iteration
        var dayCount = toDate.DayNumber - fromDate.DayNumber + 1;
        var series = new List<TicketTrendDto>(capacity: dayCount);
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            map.TryGetValue(d, out var count);
            series.Add(new TicketTrendDto(d, count));
        }

        return series;
    }

    private static IQueryable<TicketingSystem.Api.Models.Ticket> ApplyFilters(
        IQueryable<TicketingSystem.Api.Models.Ticket> query,
        DateOnly? from,
        DateOnly? to,
        byte? category)
    {
        if (from is not null)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= fromUtc);
        }

        if (to is not null)
        {
            // inclusive end date
            var toExclusive = DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt < toExclusive);
        }

        if (category is not null)
        {
            query = query.Where(t => t.CategoryId == category.Value);
        }

        return query;
    }

    private static async Task<double> GetAverageResolutionTimeHoursAsync(IQueryable<TicketingSystem.Api.Models.Ticket> query, CancellationToken ct)
    {
        var resolvedTimes = await query
            .Where(t => t.Status == TicketStatus.Resolved && t.UpdatedAt != null)
            .Select(t => new { t.CreatedAt, UpdatedAt = t.UpdatedAt!.Value })
            .ToListAsync(ct);

        if (resolvedTimes.Count == 0) return 0d;

        var avgTicks = resolvedTimes
            .Select(x => (x.UpdatedAt - x.CreatedAt).Ticks)
            .Average();

        return TimeSpan.FromTicks(Convert.ToInt64(avgTicks)).TotalHours;
    }
}
