using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Xunit;

using Microsoft.AspNetCore.Http;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.History;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Services.Tickets;
using TicketingSystem.Api.Tests.Helpers;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Tests.Services.Tickets;

public class TicketServiceTests
{
    private static TicketService CreateService(
        AppDbContext db,
        Mock<ICurrentUserService>? currentUser = null,
        Mock<IAssignmentService>? assignment = null,
        Mock<ITicketAttachmentService>? attachments = null,
        ITicketValidator? validator = null,
        ITicketUpdateValidator? updateValidator = null,
        Mock<ITicketAssignmentValidator>? assignmentValidator = null,
        Mock<IEmailService>? email = null)
    {
        if (currentUser is null)
        {
            currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(x => x.GetUserId()).Returns(2);
        }

        if (assignment is null)
        {
            assignment = new Mock<IAssignmentService>();
            assignment.Setup(x => x.ResolveAssigneeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int?)null);
        }

        attachments ??= new Mock<ITicketAttachmentService>(MockBehavior.Strict);
        attachments.Setup(a => a.AddAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()))
            .ReturnsAsync(Array.Empty<TicketFileDto>());

        validator ??= new TicketValidator(Options.Create(new TicketOptions()));
        updateValidator ??= new TicketUpdateValidator(Options.Create(new TicketOptions()));

        // Mock assignment validator to allow all assignments by default
        if (assignmentValidator is null)
        {
            assignmentValidator = new Mock<ITicketAssignmentValidator>();
            assignmentValidator.Setup(x => x.ValidateAssignmentAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        var history = new Mock<ITicketHistoryService>();
        history.Setup(x => x.LogChangeAsync(
                It.IsAny<int>(),
                It.IsAny<HistoryChangeType>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        email ??= new Mock<IEmailService>();

        var logger = new Mock<ILogger<TicketService>>();

        return new TicketService(
            db,
            currentUser.Object,
            assignment.Object,
            attachments.Object,
            validator,
            updateValidator,
            assignmentValidator.Object,
            history.Object,
            email.Object,
            logger.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTicketAndReturnsId()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2);

        var assignment = new Mock<IAssignmentService>();
        assignment.Setup(x => x.ResolveAssigneeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var attachments = new Mock<ITicketAttachmentService>();

        var service = CreateService(db, currentUser, assignment, attachments);

        var request = TestDataFactory.CreateValidTicketRequest(r =>
        {
            r.CategoryId = 1;
            r.Priority = TicketPriority.High;
            r.Files = null;
        });

        var result = await service.CreateAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.TicketId.Should().BeGreaterThan(0);
        result.AssignedToUserId.Should().Be(4);
        result.Status.Should().Be(TicketStatus.New);

        var stored = await db.Tickets.SingleAsync();
        stored.CreatedById.Should().Be(2);
        stored.Title.Should().Be(request.Title!.Trim());
        stored.Priority.Should().Be(TicketPriority.High);

        attachments.Verify(a => a.AddAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()),
            Times.Never);
        assignment.Verify(a => a.ResolveAssigneeAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTitle_ThrowsAppValidationException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        var request = TestDataFactory.CreateValidTicketRequest(r =>
        {
            r.Title = "abc"; // shorter than minimum length 6
        });

        Func<Task> act = async () => await service.CreateAsync(request, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        ex.Code.Should().Be(ErrorCodes.ValidationFailed);
        ex.Errors.Should().ContainKey("Title");
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);

        (await db.Tickets.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WhenAssignmentReturnsNull_TicketRemainsUnassigned()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var assignment = new Mock<IAssignmentService>();
        assignment.Setup(x => x.ResolveAssigneeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var service = CreateService(db, assignment: assignment);

        var request = TestDataFactory.CreateValidTicketRequest(r => r.CategoryId = 2);

        var result = await service.CreateAsync(request, CancellationToken.None);

        result.AssignedToUserId.Should().BeNull();
        var ticket = await db.Tickets.FirstAsync();
        ticket.AssignedToId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesTicketAndReturnsDetails()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        var ticket = new Ticket
        {
            TicketId = 10,
            Title = "Original Title",
            Description = new string('x', 30),
            CategoryId = 1,
            Priority = TicketPriority.Low,
            Status = TicketStatus.New,
            CreatedById = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Use Admin user for this test (can edit priority/status)
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Admin
        var service = CreateService(db, currentUser);
        var request = new UpdateTicketRequest
        {
            Title = "  Updated Title  ",
            Description = new string('y', 30),
            Priority = TicketPriority.High,
            Status = TicketStatus.Open
        };

        var dto = await service.UpdateAsync(10, request, CancellationToken.None);

        dto.Title.Should().Be("Updated Title");
        dto.Status.Should().Be((int)TicketStatus.Open);
        dto.Priority.Should().Be((int)TicketPriority.High);

        var updated = await db.Tickets.SingleAsync(t => t.TicketId == 10);
        updated.Title.Should().Be("Updated Title");
        updated.Status.Should().Be(TicketStatus.Open);
        updated.Priority.Should().Be(TicketPriority.High);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidStatusTransition_ThrowsAppException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 11,
            Title = "In Process Ticket",
            Description = new string('a', 40),
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.InProcess, // Non-terminal state
            CreatedById = 1, // Admin owns this
            AssignedToId = 1 // Admin is assigned
        });
        await db.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Admin
        var service = CreateService(db, currentUser);
        var request = new UpdateTicketRequest { Status = TicketStatus.New }; // Invalid: InProcess -> New

        Func<Task> act = async () => await service.UpdateAsync(11, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.TicketStatusTransitionInvalid)
            .WithMessage("*Illegal status change*");
    }

    [Fact]
    public async Task UpdateAsync_WhenTicketNotFound_ThrowsAppException()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        var request = new UpdateTicketRequest { Title = "Updated" };

        Func<Task> act = async () => await service.UpdateAsync(999, request, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithAssignmentRequest_UpdatesAssignee()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 12,
            Title = "Needs assignment",
            Description = new string('b', 40),
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 2
        });
        await db.SaveChangesAsync();

        // Use Admin user (can edit assignment)
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Admin
        var service = CreateService(db, currentUser);
        var request = new UpdateTicketRequest { AssignedToUserId = 4 };

        var dto = await service.UpdateAsync(12, request, CancellationToken.None);

        dto.AssignedToId.Should().Be(4);
        var updated = await db.Tickets.SingleAsync(t => t.TicketId == 12);
        updated.AssignedToId.Should().Be(4);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTicket_DeletesTicketAndRelatedData()
    {
        var (db, connection) = await TestDbContextFactory.CreateSqliteWithSeedDataAsync();
        await using var context = db;
        using var sqlite = connection;

        var ticket = new Ticket
        {
            TicketId = 13,
            Title = "Ticket to delete",
            Description = new string('c', 50),
            CategoryId = 1,
            Priority = TicketPriority.Low,
            Status = TicketStatus.New,
            CreatedById = 2
        };
        context.Tickets.Add(ticket);

        context.TicketFiles.Add(new TicketFile
        {
            TicketFileId = 1,
            TicketId = 13,
            OriginalName = "doc.txt",
            StoredName = "stored",
            ContentType = "text/plain",
            SizeBytes = 10,
            CreatedAt = DateTime.UtcNow,
            UploaderUserId = 2
        });
        context.TicketFileContents.Add(new TicketFileContent
        {
            TicketFileId = 1,
            Content = new byte[] { 1, 2, 3 }
        });
        context.TicketComments.Add(new TicketComment
        {
            CommentId = 1,
            TicketId = 13,
            Content = "Comment",
            CreatedAt = DateTime.UtcNow,
            CreatedById = 2
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteAsync(13, CancellationToken.None);

        result.Should().BeTrue();
        (await context.Tickets.AnyAsync(t => t.TicketId == 13)).Should().BeFalse();
        (await context.TicketFiles.AnyAsync()).Should().BeFalse();
        (await context.TicketFileContents.AnyAsync()).Should().BeFalse();
        (await context.TicketComments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenTicketMissing_ReturnsFalse()
    {
        var (db, connection) = await TestDbContextFactory.CreateSqliteWithSeedDataAsync();
        await using var context = db;
        using var sqlite = connection;

        var service = CreateService(context);

        var result = await service.DeleteAsync(404, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTicketExists_ReturnsDto()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.Add(new Ticket
        {
            TicketId = 20,
            Title = "Existing",
            Description = new string('d', 40),
            CategoryId = 3,
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 3
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var dto = await service.GetByIdAsync(20, CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.TicketId.Should().Be(20);
        dto.AssignedToId.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTicketMissing_ReturnsNull()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        var dto = await service.GetByIdAsync(999, CancellationToken.None);

        dto.Should().BeNull();
    }

    [Fact]
    public async Task GetListAsync_WithFilters_ReturnsFilteredResults()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.AddRange(
            new Ticket
            {
                TicketId = 30,
                Title = "Printer issue",
                Description = new string('p', 40),
                CategoryId = 3,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedById = 2
            },
            new Ticket
            {
                TicketId = 31,
                Title = "Network outage",
                Description = new string('n', 40),
                CategoryId = 1,
                Priority = TicketPriority.High,
                Status = TicketStatus.InProcess,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var query = new TicketListQuery
        {
            Status = (byte)TicketStatus.InProcess,
            CategoryId = 1, // IT category
            Page = new PageRequest { Page = 1, Size = 10 }
        };

        var result = await service.GetListAsync(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Network outage");
        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetListAsync_WithCreatedByUserIdFilter_ReturnsOnlyUserTickets()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.AddRange(
            new Ticket
            {
                TicketId = 32,
                Title = "Created by user 2",
                Description = new string('a', 40),
                CategoryId = 1,
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2
            },
            new Ticket
            {
                TicketId = 33,
                Title = "Created by user 3",
                Description = new string('b', 40),
                CategoryId = 1,
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 3
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var query = new TicketListQuery
        {
            CreatedByUserId = 2,
            Page = new PageRequest { Page = 1, Size = 10 }
        };

        var result = await service.GetListAsync(query, CancellationToken.None);

        result.Items.Should().OnlyContain(t => t.CreatedByName != null);
        result.Items.Select(t => t.TicketId).Should().Contain(32).And.NotContain(33);
    }

    [Fact]
    public async Task GetListAsync_WithAssignedToUserIdFilter_ReturnsOnlyAssignedTickets()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.AddRange(
            new Ticket
            {
                TicketId = 34,
                Title = "Assigned to 3",
                Description = new string('c', 40),
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2,
                AssignedToId = 3
            },
            new Ticket
            {
                TicketId = 35,
                Title = "Unassigned",
                Description = new string('d', 40),
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2,
                AssignedToId = null
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var query = new TicketListQuery
        {
            AssignedToUserId = 3,
            Page = new PageRequest { Page = 1, Size = 10 }
        };

        var result = await service.GetListAsync(query, CancellationToken.None);

        result.Items.Select(t => t.TicketId).Should().Contain(34).And.NotContain(35);
    }

    [Fact]
    public async Task GetListAsync_WithAssignedToIsNullFilter_ReturnsOnlyUnassignedTickets()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        db.Tickets.AddRange(
            new Ticket
            {
                TicketId = 36,
                Title = "Assigned to 3",
                Description = new string('e', 40),
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2,
                AssignedToId = 3
            },
            new Ticket
            {
                TicketId = 37,
                Title = "Unassigned",
                Description = new string('f', 40),
                CategoryId = 1,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2,
                AssignedToId = null
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var query = new TicketListQuery
        {
            AssignedToIsNull = true,
            Page = new PageRequest { Page = 1, Size = 10 }
        };

        var result = await service.GetListAsync(query, CancellationToken.None);

        result.Items.Select(t => t.TicketId).Should().Contain(37).And.NotContain(36);
    }

    [Fact]
    public async Task ExportAsync_WithFilters_ReturnsMatchingRows()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        db.Tickets.AddRange(
            new Ticket
            {
                TicketId = 40,
                Title = "Laptop setup",
                Description = new string('l', 45),
                CategoryId = 1,
                Priority = TicketPriority.Low,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CreatedById = 2
            },
            new Ticket
            {
                TicketId = 41,
                Title = "Forklift",
                Description = new string('f', 45),
                CategoryId = 2,
                Priority = TicketPriority.High,
                Status = TicketStatus.InProcess,
                CreatedAt = DateTime.UtcNow,
                CreatedById = 2
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var query = new TicketExportQuery { CategoryId = 2 }; // Logistics category

        var rows = await service.ExportAsync(query, CancellationToken.None);

        rows.Should().HaveCount(1);
        rows[0].TicketId.Should().Be(41);
    }

    [Fact]
    public async Task UpdateAsync_WithTitleChange_LogsTitleChangedHistory()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var ticket = new Ticket
        {
            Title = "Old Title",
            Description = "Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            CreatedById = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Use real history service to test actual logging
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2);
        var assignment = new Mock<IAssignmentService>();
        var attachments = new Mock<ITicketAttachmentService>(MockBehavior.Strict);
        attachments.Setup(a => a.AddAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()))
            .ReturnsAsync(Array.Empty<TicketFileDto>());

        var historyService = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());
        var email = new Mock<IEmailService>();
        var assignmentValidator = new Mock<ITicketAssignmentValidator>();
        var logger = new Mock<ILogger<TicketService>>();
        var service = new TicketService(
            db,
            currentUser.Object,
            assignment.Object,
            attachments.Object,
            new TicketValidator(Options.Create(new TicketOptions())),
            new TicketUpdateValidator(Options.Create(new TicketOptions())),
            assignmentValidator.Object,
            historyService,
            email.Object,
            logger.Object);

        var request = new UpdateTicketRequest { Title = "New Title" };

        // Act
        await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        var history = await db.TicketHistories
            .Where(h => h.TicketId == ticket.TicketId && h.ChangeType == HistoryChangeType.TitleChanged)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.OldValue.Should().Be("Old Title");
        history.NewValue.Should().Be("New Title");
    }

    [Fact]
    public async Task UpdateAsync_WithDescriptionChange_LogsDescriptionChangedHistory()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Old description that is long enough to pass validation",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            CreatedById = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Use real history service to test actual logging
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2);
        var assignment = new Mock<IAssignmentService>();
        var attachments = new Mock<ITicketAttachmentService>(MockBehavior.Strict);
        attachments.Setup(a => a.AddAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()))
            .ReturnsAsync(Array.Empty<TicketFileDto>());

        var historyService = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());
        var email = new Mock<IEmailService>();
        var assignmentValidator = new Mock<ITicketAssignmentValidator>();
        var logger = new Mock<ILogger<TicketService>>();
        var service = new TicketService(
            db,
            currentUser.Object,
            assignment.Object,
            attachments.Object,
            new TicketValidator(Options.Create(new TicketOptions())),
            new TicketUpdateValidator(Options.Create(new TicketOptions())),
            assignmentValidator.Object,
            historyService,
            email.Object,
            logger.Object);

        var request = new UpdateTicketRequest { Description = "New description that is also long enough to pass validation" };

        // Act
        await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        var history = await db.TicketHistories
            .Where(h => h.TicketId == ticket.TicketId && h.ChangeType == HistoryChangeType.DescriptionChanged)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.OldValue.Should().Be("Old description that is long enough to pass validation");
        history.NewValue.Should().Be("New description that is also long enough to pass validation");
    }

    [Fact]
    public async Task UpdateAsync_WithLongDescriptionChange_TruncatesHistoryValues()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var longOldDescription = new string('A', 200); // 200 chars
        var longNewDescription = new string('B', 200); // 200 chars
        
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = longOldDescription,
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            CreatedById = 2
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Use real history service to test actual logging
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2);
        var assignment = new Mock<IAssignmentService>();
        var attachments = new Mock<ITicketAttachmentService>(MockBehavior.Strict);
        attachments.Setup(a => a.AddAsync(
                It.IsAny<int>(),
                It.IsAny<IFormFile[]>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()))
            .ReturnsAsync(Array.Empty<TicketFileDto>());

        var historyService = new TicketHistoryService(db, TestDataFactory.CreateMockCurrentUserService());
        var email = new Mock<IEmailService>();
        var assignmentValidator = new Mock<ITicketAssignmentValidator>();
        var logger = new Mock<ILogger<TicketService>>();
        var service = new TicketService(
            db,
            currentUser.Object,
            assignment.Object,
            attachments.Object,
            new TicketValidator(Options.Create(new TicketOptions())),
            new TicketUpdateValidator(Options.Create(new TicketOptions())),
            assignmentValidator.Object,
            historyService,
            email.Object,
            logger.Object);

        var request = new UpdateTicketRequest { Description = longNewDescription };

        // Act
        await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        var history = await db.TicketHistories
            .Where(h => h.TicketId == ticket.TicketId && h.ChangeType == HistoryChangeType.DescriptionChanged)
            .FirstOrDefaultAsync();

        history.Should().NotBeNull();
        history!.OldValue.Should().NotBeNull();
        history.NewValue.Should().NotBeNull();
        // Verify truncation occurred (max 100 chars + "...")
        history.OldValue!.Length.Should().Be(103); // Exactly 100 + "..."
        history.NewValue!.Length.Should().Be(103); // Exactly 100 + "..."
        history.OldValue.Should().EndWith("...");
        history.NewValue.Should().EndWith("...");
        history.OldValue.Should().StartWith("AAAA"); // First chars preserved
        history.NewValue.Should().StartWith("BBBB"); // First chars preserved
    }

    #region Permission Enforcement Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsCapabilities()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2, // Same as current user
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(ticket.TicketId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Capabilities.Should().NotBeNull();
        result.Capabilities!.CanEdit.Should().BeTrue("employee should edit their own ticket in New state");
        result.Capabilities.CanEditTitle.Should().BeTrue();
        result.Capabilities.CanEditDescription.Should().BeTrue();
        result.Capabilities.CanEditCategory.Should().BeFalse("employee cannot edit category");
        result.Capabilities.CanEditPriority.Should().BeFalse("employee cannot edit priority");
        result.Capabilities.CanEditStatus.Should().BeFalse("employee cannot edit status");
        result.Capabilities.CanEditAssignment.Should().BeFalse("employee cannot edit assignment");
    }

    [Fact]
    public async Task UpdateAsync_WhenUserCannotEdit_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.InProcess,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Title = "New Title" };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("do not have permission to edit"));
    }

    [Fact]
    public async Task UpdateAsync_WhenUserCannotEditTitle_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Resolved, // Terminal state - no edits allowed
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Title = "New Title" };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeAttemptsToEditCategory_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New, // Mutable state for employee
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { CategoryId = 2 }; // Employee cannot change category

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("category"));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeAttemptsToEditPriority_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Priority = TicketPriority.High };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("priority"));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeAttemptsToEditStatus_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Status = TicketStatus.Open };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("status"));
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeAttemptsToEditAssignment_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { AssignedToUserId = 3 };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("assignment"));
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminEditsResolvedTicket_ThrowsForbidden()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Admin user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Resolved, // Terminal state
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Title = "New Title" };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ForbiddenOperation &&
                         ex.Message.Contains("do not have permission to edit"));
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminEditsOpenTicket_Succeeds()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Admin user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Old Title",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Title = "New Title" };

        // Act
        var result = await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        result.Capabilities.Should().NotBeNull();
        result.Capabilities!.CanEdit.Should().BeTrue("admin should be able to edit");
    }

    [Fact]
    public async Task UpdateAsync_WhenEmployeeEditsOwnTicketDescription_Succeeds()
    {
        // Arrange
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user

        var service = CreateService(db, currentUser);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Old Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2, // Created by same employee
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var request = new UpdateTicketRequest { Description = "New Description that is long enough to pass validation" };

        // Act
        var result = await service.UpdateAsync(ticket.TicketId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("New Description that is long enough to pass validation");
        result.Capabilities.Should().NotBeNull();
        result.Capabilities!.CanEditDescription.Should().BeTrue();
    }

    #endregion

    #region GetAssignableUsersAsync Tests

    [Fact]
    public async Task GetAssignableUsersAsync_WithNonExistentTicket_ThrowsTicketNotFound()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Act
        Func<Task> act = async () => await service.GetAssignableUsersAsync(9999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task GetAssignableUsersAsync_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();

        // Create category
        var category = new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        // Add active and inactive users with Support role
        var activeUser = new User
        {
            UserId = 10,
            Name = "Active Support",
            Email = "active@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = true
        };
        var inactiveUser = new User
        {
            UserId = 11,
            Name = "Inactive Support",
            Email = "inactive@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = false
        };
        db.Users.AddRange(activeUser, inactiveUser);

        // Create ticket in IT category
        var ticket = new Ticket
        {
            TicketId = 100,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 10,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAssignableUsersAsync(100, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain(u => u.UserId == 11, "inactive users should be excluded");
        result.Should().Contain(u => u.UserId == 10, "active users should be included");
    }

    [Fact]
    public async Task GetAssignableUsersAsync_ReturnsOnlyEligibleRoles()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();

        // Create category
        var category = new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        // Add users with different roles
        var supportUser = new User
        {
            UserId = 10,
            Name = "Support User",
            Email = "support@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = true
        };
        var teamLeaderUser = new User
        {
            UserId = 11,
            Name = "TeamLeader User",
            Email = "teamleader@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.TeamLeader,
            CategoryId = 1,
            IsActive = true
        };
        var adminUser = new User
        {
            UserId = 12,
            Name = "Admin User",
            Email = "admin@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Admin,
            IsActive = true
        };
        var employeeUser = new User
        {
            UserId = 13,
            Name = "Employee User",
            Email = "employee@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Employee,
            CategoryId = 1,
            IsActive = true
        };
        db.Users.AddRange(supportUser, teamLeaderUser, adminUser, employeeUser);

        // Create ticket in IT category
        var ticket = new Ticket
        {
            TicketId = 100,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAssignableUsersAsync(100, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(u => u.UserId == 10, "Support should be included");
        result.Should().Contain(u => u.UserId == 11, "TeamLeader should be included");
        result.Should().Contain(u => u.UserId == 12, "Admin should be included");
        result.Should().NotContain(u => u.UserId == 13, "Employee should be excluded");
    }

    [Fact]
    public async Task GetAssignableUsersAsync_FiltersByCategoryForSupport()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();

        // Create categories
        var itCategory = new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT" };
        var logisticsCategory = new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics" };
        db.Categories.AddRange(itCategory, logisticsCategory);
        await db.SaveChangesAsync();

        // Add Support users in different categories
        var itSupport = new User
        {
            UserId = 10,
            Name = "IT Support",
            Email = "itsupport@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = true
        };
        var logisticsSupport = new User
        {
            UserId = 11,
            Name = "Logistics Support",
            Email = "logisticssupport@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 2,
            IsActive = true
        };
        db.Users.AddRange(itSupport, logisticsSupport);

        // Create ticket in IT category
        var ticket = new Ticket
        {
            TicketId = 100,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAssignableUsersAsync(100, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(u => u.UserId == 10, "IT Support should be included for IT ticket");
        result.Should().NotContain(u => u.UserId == 11, "Logistics Support should be excluded from IT ticket");
    }

    [Fact]
    public async Task GetAssignableUsersAsync_IncludesAdminFromAnyCategory()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();

        // Create categories
        var itCategory = new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT" };
        var logisticsCategory = new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics" };
        db.Categories.AddRange(itCategory, logisticsCategory);
        await db.SaveChangesAsync();

        // Add Admin users with different CategoryId (or null)
        var adminNoCategory = new User
        {
            UserId = 10,
            Name = "Admin No Category",
            Email = "admin1@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Admin,
            CategoryId = null,
            IsActive = true
        };
        var adminWithCategory = new User
        {
            UserId = 11,
            Name = "Admin With Category",
            Email = "admin2@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Admin,
            CategoryId = 2, // Different category
            IsActive = true
        };
        db.Users.AddRange(adminNoCategory, adminWithCategory);

        // Create ticket in IT category
        var ticket = new Ticket
        {
            TicketId = 100,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAssignableUsersAsync(100, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(u => u.UserId == 10, "Admin with no category should be included");
        result.Should().Contain(u => u.UserId == 11, "Admin with different category should be included (exempt from category filter)");
    }

    [Fact]
    public async Task GetAssignableUsersAsync_SortsByRoleThenName()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();

        // Create category
        var category = new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        // Add users with different roles and names (reverse alphabetical order to test sorting)
        var supportZ = new User
        {
            UserId = 10,
            Name = "Zara Support",
            Email = "zara@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = true
        };
        var supportA = new User
        {
            UserId = 11,
            Name = "Anna Support",
            Email = "anna@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = true
        };
        var teamLeaderB = new User
        {
            UserId = 12,
            Name = "Bob TeamLeader",
            Email = "bob@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.TeamLeader,
            CategoryId = 1,
            IsActive = true
        };
        var adminC = new User
        {
            UserId = 13,
            Name = "Charlie Admin",
            Email = "charlie@test.local",
            PasswordHash = "hash",
            RoleId = UserRole.Admin,
            IsActive = true
        };
        db.Users.AddRange(supportZ, supportA, teamLeaderB, adminC);

        // Create ticket in IT category
        var ticket = new Ticket
        {
            TicketId = 100,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAssignableUsersAsync(100, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var resultList = result.ToList();
        
        // Admin should be first (highest role)
        resultList[0].UserId.Should().Be(13, "Admin should be first");
        
        // TeamLeader should be second
        resultList[1].UserId.Should().Be(12, "TeamLeader should be second");
        
        // Support users sorted alphabetically by name
        var supportUsers = resultList.Skip(2).ToList();
        supportUsers[0].UserId.Should().Be(11, "Anna Support should be before Zara Support");
        supportUsers[1].UserId.Should().Be(10, "Zara Support should be last");
    }

    #endregion
}
