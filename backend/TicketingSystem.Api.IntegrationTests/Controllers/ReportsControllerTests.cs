using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

using FluentAssertions;

using TicketingSystem.Api.DTOs.Reports;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.IntegrationTests.Helpers;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;

using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class ReportsControllerTests
{
    private readonly PostgresTestContainer _postgres;

    public ReportsControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Dashboard_AsAdmin_ReturnsStats()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "admin.reports@test.local";
        const string password = "Admin#123";
        await SeedUserAsync(ctx, "Admin", email, password, UserRole.Admin);
        await AuthenticateAsync(ctx, email, password);

        await SeedTicketsAsync(ctx);

        var response = await ctx.Client.GetAsync("/api/reports/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();
        dto.Should().NotBeNull();
        dto!.TotalTickets.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TicketsByCategory_AsTeamLeader_IsScopedToUserCategory()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        const string email = "leader.reports@test.local";
        const string password = "Leader#123";
        await SeedUserAsync(ctx, "Leader", email, password, UserRole.TeamLeader, 2); // Logistics
        await AuthenticateAsync(ctx, email, password);

        await SeedTicketsAsync(ctx);

        var response = await ctx.Client.GetAsync("/api/reports/tickets-by-category");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Unexpected status: {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }

        var rows = await response.Content.ReadFromJsonAsync<List<TicketCountByCategoryDto>>();
        rows.Should().NotBeNull();
        rows!.Should().NotBeEmpty();
        rows.Should().OnlyContain(r => r.CategoryId == (byte)TicketCategory.Logistics);
    }

    private static async Task SeedTicketsAsync(IntegrationTestContext ctx)
    {
        await ctx.WithDbContextAsync(async db =>
        {
            if (db.Tickets.Any()) return;

            var employee = new User
            {
                Name = "Employee",
                Email = "employee.reports@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.Employee,
                CategoryId = 1
            };
            db.Users.Add(employee);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            db.Tickets.AddRange(
                new Ticket { Title = "IT New", Description = "d", CategoryId = 1, Priority = TicketPriority.Low, Status = TicketStatus.New, CreatedAt = now.AddDays(-2), CreatedById = employee.UserId },
                new Ticket { Title = "Log InProcess", Description = "d", CategoryId = 2, Priority = TicketPriority.Low, Status = TicketStatus.InProcess, CreatedAt = now.AddDays(-1), CreatedById = employee.UserId },
                new Ticket { Title = "Log Resolved", Description = "d", CategoryId = 2, Priority = TicketPriority.Low, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-5), CreatedById = employee.UserId }
            );

            await db.SaveChangesAsync();
        });
    }

    private static async Task<int> SeedUserAsync(IntegrationTestContext ctx, string name, string email, string password, UserRole role, int? category = null)
    {
        return await ctx.WithDbContextAsync(async db =>
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                RoleId = role,
                CategoryId = category
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.UserId;
        });
    }

    private static async Task AuthenticateAsync(IntegrationTestContext ctx, string email, string password)
    {
        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var authCookie = cookies!.First(c => c.StartsWith("auth_token=", StringComparison.Ordinal));
        var token = authCookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First()
            .Substring("auth_token=".Length);

        ctx.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

