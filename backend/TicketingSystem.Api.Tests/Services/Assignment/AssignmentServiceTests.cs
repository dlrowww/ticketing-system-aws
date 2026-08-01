using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Tests.Helpers;

namespace TicketingSystem.Api.Tests.Services.Assignment;

public class AssignmentServiceTests
{
    [Fact]
    public async Task ResolveAssigneeAsync_WithTeamLeader_ReturnsLeaderId()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        var existing = await db.Users
            .Where(u => u.RoleId == UserRole.TeamLeader && u.CategoryId == 2)
            .ToListAsync();
        db.Users.RemoveRange(existing);
        await db.SaveChangesAsync();

        var teamLeader = new User
        {
            UserId = 100,
            Name = "TL Logistics",
            Email = "tl.log@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.TeamLeader,
            CategoryId = 2
        };
        db.Users.Add(teamLeader);
        await db.SaveChangesAsync();

        var service = new AssignmentService(db);
        var result = await service.ResolveAssigneeAsync(2, CancellationToken.None); // Logistics

        result.Should().Be(100);
    }

    [Fact]
    public async Task ResolveAssigneeAsync_WhenNoTeamLeader_ReturnsNull()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        // Remove all team leaders for Administration (CategoryId = 3)
        var leaders = await db.Users.Where(u => u.RoleId == UserRole.TeamLeader && u.CategoryId == 3).ToListAsync();
        db.Users.RemoveRange(leaders);
        await db.SaveChangesAsync();

        var service = new AssignmentService(db);
        var result = await service.ResolveAssigneeAsync(3, CancellationToken.None); // Administration

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAssigneeAsync_WithMultipleTeamLeaders_ReturnsDeterministicFirst()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Users.AddRange(
            new User
            {
                UserId = 200,
                Name = "Leader One",
                Email = "leader.one@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.TeamLeader,
                CategoryId = 1
            },
            new User
            {
                UserId = 201,
                Name = "Leader Two",
                Email = "leader.two@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.TeamLeader,
                CategoryId = 1
            });
        await db.SaveChangesAsync();

        var service = new AssignmentService(db);
        var result = await service.ResolveAssigneeAsync(1, CancellationToken.None); // IT

        result.Should().Be(4); // seeded TeamLeader IT has UserId = 4 in TestDbContextFactory
    }
}
