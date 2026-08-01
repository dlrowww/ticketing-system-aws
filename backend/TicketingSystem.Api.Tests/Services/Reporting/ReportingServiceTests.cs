using FluentAssertions;
using System.Linq;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Reporting;
using TicketingSystem.Api.Tests.Helpers;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.Reporting;

public sealed class ReportingServiceTests
{
    [Fact]
    public async Task GetDashboardStatsAsync_WithMixedTickets_ReturnsCountsAndAverageResolutionTime()
    {
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var now = DateTime.UtcNow;

        db.Tickets.AddRange(
            new Ticket { Title = "t1", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.New, CreatedAt = now.AddDays(-2), CreatedById = 2 },
            new Ticket { Title = "t2", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.Open, CreatedAt = now.AddDays(-2), CreatedById = 2 },
            new Ticket { Title = "t3", Description = "d", CategoryId = 2, Priority = TicketPriority.Low, Status = TicketStatus.InProcess, CreatedAt = now.AddDays(-1), CreatedById = 2 },
            new Ticket { Title = "t4", Description = "d", CategoryId = 2, Priority = TicketPriority.Low, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-1), CreatedById = 2 }
        );
        await db.SaveChangesAsync();

        var svc = new ReportingService(db);

        var from = DateOnly.FromDateTime(now.AddDays(-10));
        var to = DateOnly.FromDateTime(now);
        var stats = await svc.GetDashboardStatsAsync(from, to, category: null, CancellationToken.None);

        stats.TotalTickets.Should().BeGreaterOrEqualTo(4);
        stats.OpenTickets.Should().Be(2);
        stats.InProgressTickets.Should().Be(1);
        stats.ResolvedTickets.Should().Be(1);
        stats.AverageResolutionTimeHours.Should().BeApproximately(96, precision: 0.5); // 4 days
    }

    [Fact]
    public async Task GetTicketsByCategoryAsync_WithCategoryScope_FiltersToThatCategory()
    {
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var now = DateTime.UtcNow;

        db.Tickets.AddRange(
            new Ticket { Title = "it", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.New, CreatedAt = now.AddDays(-1), CreatedById = 2 },
            new Ticket { Title = "log", Description = "d", CategoryId = 2, Priority = TicketPriority.Low, Status = TicketStatus.New, CreatedAt = now.AddDays(-1), CreatedById = 2 }
        );
        await db.SaveChangesAsync();

        var svc = new ReportingService(db);
        var from = DateOnly.FromDateTime(now.AddDays(-7));
        var to = DateOnly.FromDateTime(now);

        var rows = await svc.GetTicketsByCategoryAsync(from, to, category: 1, CancellationToken.None); // IT category

        rows.Should().HaveCount(1);
        rows[0].CategoryId.Should().Be(1);
        rows[0].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTicketTrendAsync_WhenNoTickets_ReturnsZeroesForAllDays()
    {
        var db = TestDbContextFactory.CreateInMemory();
        var svc = new ReportingService(db);

        var rows = await svc.GetTicketTrendAsync(null, null, 7, null, CancellationToken.None);

        rows.Should().HaveCount(7);
        rows.Should().OnlyContain(x => x.Count == 0);
    }

    [Fact]
    public async Task GetTicketsByStatusAsync_ReturnsIndividualCountsForCancelledAndReturned()
    {
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var now = DateTime.UtcNow;

        db.Tickets.AddRange(
            new Ticket { Title = "cancelled", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.Cancelled, CreatedAt = now.AddDays(-3), CreatedById = 2 },
            new Ticket { Title = "returned", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.Returned, CreatedAt = now.AddDays(-2), CreatedById = 2 }
        );
        await db.SaveChangesAsync();

        var svc = new ReportingService(db);
        var rows = await svc.GetTicketsByStatusAsync(from: null, to: null, category: null, CancellationToken.None);

        rows.Single(r => r.Status == (byte)TicketStatus.Cancelled).Count.Should().Be(1);
        rows.Single(r => r.Status == (byte)TicketStatus.Returned).Count.Should().Be(1);
    }
}
