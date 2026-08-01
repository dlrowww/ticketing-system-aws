using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Tests.Helpers;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Tests.Services.Comments;

public class CommentServiceTests
{
    private static CommentService CreateService(AppDbContext db, int currentUserId = 2)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.GetUserId()).Returns(currentUserId);

        var validator = new CommentValidator(Options.Create(new CommentOptions()));
        var email = new Mock<IEmailService>();
        var history = new Mock<ITicketHistoryService>();
        var logger = new Mock<ILogger<CommentService>>();
        
        return new CommentService(db, currentUser.Object, validator, email.Object, history.Object, logger.Object);
    }

    [Fact]
    public async Task AddAsync_WithValidContent_CreatesComment()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.Add(new Ticket
        {
            TicketId = 501,
            Title = "Ticket",
            Description = new string('x', 40),
            CategoryId = 1, // IT category (already seeded)
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUserId: 2);
        var request = new AddCommentRequest { Content = "Looks good" };

        var dto = await service.AddAsync(501, request, CancellationToken.None);

        dto.Content.Should().Be("Looks good");
        dto.TicketId.Should().Be(501);
        dto.CreatedById.Should().Be(2);
        dto.CommentId.Should().BeGreaterThan(0);
        dto.IsInternal.Should().BeFalse();
        dto.CreatedByRoleId.Should().Be(TicketingSystem.Api.Enums.Identity.UserRole.Employee);

        (await db.TicketComments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_WithIsInternalTrue_BySupport_CreatesInternalComment()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.Add(new Ticket
        {
            TicketId = 505,
            Title = "Ticket",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUserId: 3); // Support
        var request = new AddCommentRequest { Content = "Internal note", IsInternal = true };

        var dto = await service.AddAsync(505, request, CancellationToken.None);

        dto.IsInternal.Should().BeTrue();
        dto.CreatedByRoleId.Should().Be(TicketingSystem.Api.Enums.Identity.UserRole.Support);

        var saved = await db.TicketComments.SingleAsync(c => c.CommentId == dto.CommentId);
        saved.IsInternal.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WithIsInternalTrue_ByEmployee_ThrowsAppException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.Add(new Ticket
        {
            TicketId = 506,
            Title = "Ticket",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUserId: 2); // Employee
        var request = new AddCommentRequest { Content = "Attempt internal", IsInternal = true };

        Func<Task> act = async () => await service.AddAsync(506, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CommentInternalNotAllowed);
    }

    [Fact]
    public async Task AddAsync_WhenTicketNotFound_ThrowsAppException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        var request = new AddCommentRequest { Content = "Missing ticket" };

        Func<Task> act = async () => await service.AddAsync(999, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task AddAsync_WithEmptyContent_ThrowsAppException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 502,
            Title = "Ticket",
            Description = new string('x', 40),
            CategoryId = 1, // IT category (already seeded)
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new AddCommentRequest { Content = "   " };

        Func<Task> act = async () => await service.AddAsync(502, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CommentEmpty);
    }

    [Fact]
    public async Task ListAsync_ReturnsCommentsOrderedByCreatedAt()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 503,
            Title = "Ticket",
            Description = new string('y', 40),
            CategoryId = 1, // IT category (already seeded)
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        db.TicketComments.AddRange(
            new TicketComment
            {
                CommentId = 10,
                TicketId = 503,
                Content = "first",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                CreatedById = 2,
                IsInternal = false
            },
            new TicketComment
            {
                CommentId = 11,
                TicketId = 503,
                Content = "second",
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2,
                IsInternal = false
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var list = await service.ListAsync(503, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].Content.Should().Be("first");
        list[1].Content.Should().Be("second");
    }

    [Fact]
    public async Task ListAsync_WhenEmployee_HidesInternalComments()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 507,
            Title = "Ticket",
            Description = new string('y', 40),
            CategoryId = 1,
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        db.TicketComments.AddRange(
            new TicketComment
            {
                TicketId = 507,
                Content = "public",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedById = 3,
                IsInternal = false
            },
            new TicketComment
            {
                TicketId = 507,
                Content = "internal",
                CreatedAt = DateTime.UtcNow,
                CreatedById = 3,
                IsInternal = true
            });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUserId: 2); // Employee
        var list = await service.ListAsync(507, CancellationToken.None);

        list.Should().HaveCount(1);
        list[0].Content.Should().Be("public");
        list[0].IsInternal.Should().BeFalse();
        list[0].CreatedByRoleId.Should().Be(TicketingSystem.Api.Enums.Identity.UserRole.Support);
    }

    [Fact]
    public async Task ListAsync_WhenSupport_IncludesInternalComments()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 508,
            Title = "Ticket",
            Description = new string('y', 40),
            CategoryId = 1,
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Medium,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        db.TicketComments.AddRange(
            new TicketComment
            {
                TicketId = 508,
                Content = "public",
                CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                CreatedById = 3,
                IsInternal = false
            },
            new TicketComment
            {
                TicketId = 508,
                Content = "internal",
                CreatedAt = DateTime.UtcNow,
                CreatedById = 3,
                IsInternal = true
            });
        await db.SaveChangesAsync();

        var service = CreateService(db, currentUserId: 3); // Support
        var list = await service.ListAsync(508, CancellationToken.None);

        list.Should().HaveCount(2);
        list.Any(c => c.IsInternal).Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_WithNoComments_ReturnsEmptyList()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 504,
            Title = "Ticket",
            Description = new string('z', 40),
            CategoryId = 1, // IT category (already seeded)
            Priority = TicketingSystem.Api.Enums.Tickets.TicketPriority.Low,
            Status = TicketingSystem.Api.Enums.Tickets.TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var list = await service.ListAsync(504, CancellationToken.None);

        list.Should().BeEmpty();
    }
}
