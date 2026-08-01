using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

using TicketingSystem.Api.DTOs.Reports;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Services.Reporting;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TeamLeader")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportingService _reporting;

    public ReportsController(IReportingService reporting)
    {
        _reporting = reporting;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var categoryScope = GetCategoryScopeForCurrentUser();
        var dto = await _reporting.GetDashboardStatsAsync(from, to, categoryScope, ct);
        return Ok(dto);
    }

    [HttpGet("tickets-by-category")]
    public async Task<ActionResult<IReadOnlyList<TicketCountByCategoryDto>>> GetTicketsByCategory(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var categoryScope = GetCategoryScopeForCurrentUser();
        var rows = await _reporting.GetTicketsByCategoryAsync(from, to, categoryScope, ct);
        return Ok(rows);
    }

    [HttpGet("tickets-by-status")]
    public async Task<ActionResult<IReadOnlyList<TicketCountByStatusDto>>> GetTicketsByStatus(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var categoryScope = GetCategoryScopeForCurrentUser();
        var rows = await _reporting.GetTicketsByStatusAsync(from, to, categoryScope, ct);
        return Ok(rows);
    }

    [HttpGet("tickets-by-priority")]
    public async Task<ActionResult<IReadOnlyList<TicketCountByPriorityDto>>> GetTicketsByPriority(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var categoryScope = GetCategoryScopeForCurrentUser();
        var rows = await _reporting.GetTicketsByPriorityAsync(from, to, categoryScope, ct);
        return Ok(rows);
    }

    [HttpGet("ticket-trend")]
    public async Task<ActionResult<IReadOnlyList<TicketTrendDto>>> GetTicketTrend(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var categoryScope = GetCategoryScopeForCurrentUser();
        var rows = await _reporting.GetTicketTrendAsync(from, to, days, categoryScope, ct);
        return Ok(rows);
    }

    private byte? GetCategoryScopeForCurrentUser()
    {
        // Admin can see all categories. TeamLeader is scoped to their CategoryId.
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(role, UserRole.TeamLeader.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var raw = User.FindFirst("categoryId")?.Value;
            if (byte.TryParse(raw, out var categoryId)) return categoryId;
            return null;
        }

        return null;
    }
}
