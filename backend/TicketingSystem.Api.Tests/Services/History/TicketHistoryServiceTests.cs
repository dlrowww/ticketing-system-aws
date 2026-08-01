using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Tests.Helpers;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.History;

public class TicketHistoryServiceTests
{
    [Fact]
    public async Task LogChangeAsync_WithValidData_CreatesHistoryEntry()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.TicketCreated,
            oldValue: null,
            newValue: "Status: New | Category: IT | Priority: Medium",
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories
            .Where(h => h.TicketId == ticket.TicketId)
            .ToListAsync();

        history.Should().HaveCount(1);
        history[0].ChangeType.Should().Be(HistoryChangeType.TicketCreated);
        history[0].OldValue.Should().BeNull();
        history[0].NewValue.Should().Be("Status: New | Category: IT | Priority: Medium");
        history[0].ChangedById.Should().Be(1);
    }

    [Fact]
    public async Task LogChangeAsync_WithStatusChange_LogsOldAndNewValues()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.StatusChanged,
            oldValue: "Open",
            newValue: "Resolved",
            changedBy: 2,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.StatusChanged);
        history.OldValue.Should().Be("Open");
        history.NewValue.Should().Be("Resolved");
        history.ChangedById.Should().Be(2);
    }

    [Fact]
    public async Task LogChangeAsync_WithAssignmentChange_LogsUserIds()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1,
            AssignedToId = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act - Reassign from user 2 to user 3
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.AssignmentChanged,
            oldValue: "2",
            newValue: "3",
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.AssignmentChanged);
        history.OldValue.Should().Be("2");
        history.NewValue.Should().Be("3");
    }

    [Fact]
    public async Task LogChangeAsync_WithNullOldValue_HandlesCreationEvent()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "New Ticket",
            Description = "Description",
            CategoryId = 2, // Logistics
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedById = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act - Initial assignment (no previous value)
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.AssignmentChanged,
            oldValue: null,
            newValue: "3",
            changedBy: 2,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.OldValue.Should().BeNull();
        history.NewValue.Should().Be("3");
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyList_WhenNoHistoryExists()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Ticket Without History",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Low,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        var history = await service.GetHistoryAsync(ticket.TicketId, CancellationToken.None);

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryInChronologicalOrder()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act - Log multiple changes
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.TicketCreated, null, "Status: New", 1, CancellationToken.None);
        await Task.Delay(10); // Ensure different timestamps
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.StatusChanged, "New", "Open", 1, CancellationToken.None);
        await Task.Delay(10);
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.PriorityChanged, "Medium", "High", 1, CancellationToken.None);

        // Act
        var history = await service.GetHistoryAsync(ticket.TicketId, CancellationToken.None);

        // Assert
        history.Should().HaveCount(3);
        history[0].ChangeType.Should().Be("TicketCreated"); // Oldest first
        history[1].ChangeType.Should().Be("StatusChanged");
        history[2].ChangeType.Should().Be("PriorityChanged");

        // Verify chronological ordering
        for (int i = 0; i < history.Count - 1; i++)
        {
            history[i].ChangedAt.Should().BeBefore(history[i + 1].ChangedAt);
        }
    }

    [Fact]
    public async Task GetHistoryAsync_IncludesChangedByUserName()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act - Log change by user with ID 1 (Test Admin from seed data)
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.StatusChanged,
            "New",
            "Open",
            changedBy: 1,
            CancellationToken.None
        );

        // Act
        var history = await service.GetHistoryAsync(ticket.TicketId, CancellationToken.None);

        // Assert
        history.Should().HaveCount(1);
        history[0].ChangedByName.Should().Be("Test Admin"); // From seed data
    }

    [Fact]
    public async Task GetHistoryAsync_HandlesMultipleChangesToSameTicket()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act - Log complete lifecycle
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.TicketCreated, null, "Status: New", 1, CancellationToken.None);
        await Task.Delay(10);
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.AssignmentChanged, null, "3", 1, CancellationToken.None);
        await Task.Delay(10);
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.StatusChanged, "New", "Open", 3, CancellationToken.None);
        await Task.Delay(10);
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.PriorityChanged, "Medium", "High", 3, CancellationToken.None);
        await Task.Delay(10);
        await service.LogChangeAsync(ticket.TicketId, HistoryChangeType.StatusChanged, "Open", "Resolved", 3, CancellationToken.None);

        // Act
        var history = await service.GetHistoryAsync(ticket.TicketId, CancellationToken.None);

        // Assert
        history.Should().HaveCount(5);
        history.Select(h => h.ChangeType).Should().ContainInOrder("TicketCreated", "AssignmentChanged", "StatusChanged", "PriorityChanged", "StatusChanged");
    }

    [Fact]
    public async Task GetHistoryAsync_OnlyReturnsHistoryForSpecifiedTicket()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket1 = new Ticket
        {
            Title = "Ticket 1",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 1
        };
        var ticket2 = new Ticket
        {
            Title = "Ticket 2",
            Description = "Description",
            CategoryId = 2, // Logistics
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedById = 2
        };
        db.Tickets.AddRange(ticket1, ticket2);
        await db.SaveChangesAsync();

        // Act - Log history for both tickets
        await service.LogChangeAsync(ticket1.TicketId, HistoryChangeType.TicketCreated, null, "Status: New", 1, CancellationToken.None);
        await service.LogChangeAsync(ticket2.TicketId, HistoryChangeType.TicketCreated, null, "Status: New", 2, CancellationToken.None);
        await service.LogChangeAsync(ticket1.TicketId, HistoryChangeType.StatusChanged, "New", "Open", 1, CancellationToken.None);

        // Act - Get history for ticket1 only
        var history = await service.GetHistoryAsync(ticket1.TicketId, CancellationToken.None);

        // Assert
        history.Should().HaveCount(2);
        history.All(h => h.TicketId == ticket1.TicketId).Should().BeTrue();
    }

    [Fact]
    public async Task LogChangeAsync_WithCategoryChange_LogsCorrectly()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.CategoryChanged,
            oldValue: "IT",
            newValue: "Logistics",
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.CategoryChanged);
        history.OldValue.Should().Be("IT");
        history.NewValue.Should().Be("Logistics");
    }

    [Fact]
    public async Task LogChangeAsync_WithPriorityChange_LogsCorrectly()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Low,
            Status = TicketStatus.Open,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.PriorityChanged,
            oldValue: "Low",
            newValue: "Critical",
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.PriorityChanged);
        history.OldValue.Should().Be("Low");
        history.NewValue.Should().Be("Critical");
    }

    [Fact]
    public async Task LogChangeAsync_WithTitleChange_LogsCorrectly()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Old Title",
            Description = "Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.TitleChanged,
            oldValue: "Old Title",
            newValue: "New Title",
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.TitleChanged);
        history.OldValue.Should().Be("Old Title");
        history.NewValue.Should().Be("New Title");
    }

    [Fact]
    public async Task LogChangeAsync_WithDescriptionChange_LogsTruncatedValues()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Old description text",
            CategoryId = 1, // IT
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 1
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var longOldDescription = new string('A', 150) + "..."; // Simulating truncated old value
        var longNewDescription = new string('B', 150) + "..."; // Simulating truncated new value

        // Act
        await service.LogChangeAsync(
            ticket.TicketId,
            HistoryChangeType.DescriptionChanged,
            oldValue: longOldDescription,
            newValue: longNewDescription,
            changedBy: 1,
            CancellationToken.None
        );

        // Assert
        var history = await db.TicketHistories.FirstOrDefaultAsync(h => h.TicketId == ticket.TicketId);
        history.Should().NotBeNull();
        history!.ChangeType.Should().Be(HistoryChangeType.DescriptionChanged);
        history.OldValue.Should().Be(longOldDescription);
        history.NewValue.Should().Be(longNewDescription);
        history.OldValue!.Length.Should().BeLessOrEqualTo(500); // Database column limit
        history.NewValue!.Length.Should().BeLessOrEqualTo(500); // Database column limit
    }
}






