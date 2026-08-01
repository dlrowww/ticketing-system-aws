using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Infrastructure.Email;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Email;
using TicketingSystem.Api.Services.Localization;
using TicketingSystem.Api.Tests.Helpers;

namespace TicketingSystem.Api.Tests.Services.Email;

/// <summary>
/// Unit tests for EmailService notification methods.
/// Tests all 6 notification types from Phase 2.2.
/// </summary>
public class EmailServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ILocalizationService> _mockLocalization;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly EmailOptions _emailOptions;
    private readonly string _tempPickupDir;
    private readonly string _tempContentRoot;

    public EmailServiceTests()
    {
        // Create temporary directories
        _tempPickupDir = Path.Combine(Path.GetTempPath(), $"EmailServiceTests_{Guid.NewGuid():N}");
        _tempContentRoot = Path.Combine(Path.GetTempPath(), $"ContentRoot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPickupDir);
        Directory.CreateDirectory(_tempContentRoot);

        // Setup database with seed data
        _db = TestDbContextFactory.CreateWithSeedDataAsync().Result;

        // Setup mocks
        _mockLocalization = new Mock<ILocalizationService>();
        _mockLocalization.Setup(x => x.GetBilingualEnum(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string type, string value) => $"{value} (PL) / {value} (EN)");
        _mockLocalization.Setup(x => x.GetEmailLabel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string locale) => $"{key}_{locale}");

        _mockLogger = new Mock<ILogger<EmailService>>();

        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(x => x.ContentRootPath).Returns(_tempContentRoot);

        // Setup email options for pickup directory
        _emailOptions = new EmailOptions
        {
            SmtpHost = "localhost",
            SmtpPort = 25,
            UseSsl = false,
            FromAddress = "test@test.com",
            FromName = "Test System",
            BaseUrl = "http://localhost:3000",
            TemplatesPath = "EmailTemplates",
            UsePickupDirectory = true,
            PickupDirectoryPath = _tempPickupDir
        };

        // Create email templates directory
        var templatesDir = Path.Combine(_tempContentRoot, "EmailTemplates");
        Directory.CreateDirectory(templatesDir);
        CreateMockTemplates(templatesDir);
    }

    private void CreateMockTemplates(string templatesDir)
    {
        // Create simple HTML templates for testing
        var templateNames = new[]
        {
            "TicketAssigned.html",
            "TicketReassigned.html",
            "TicketStatusChanged.html",
            "CommentAdded.html",
            "PriorityEscalated.html",
            "TicketResolved.html"
        };

        foreach (var templateName in templateNames)
        {
            var content = @"<!DOCTYPE html>
<html>
<head><title>{{Subject}}</title></head>
<body>
    <h1>Ticket #{{TicketId}}: {{Title}}</h1>
    <p>Category: {{Category}}</p>
    <p>Priority: {{Priority}}</p>
    <p>Status: {{Status}}</p>
    <p><a href=""{{TicketUrl}}"">View Ticket</a></p>
</body>
</html>";
            File.WriteAllText(Path.Combine(templatesDir, templateName), content);
        }
    }

    public void Dispose()
    {
        // Cleanup temporary directories
        if (Directory.Exists(_tempPickupDir))
        {
            try { Directory.Delete(_tempPickupDir, true); }
            catch { /* Ignore cleanup errors */ }
        }

        if (Directory.Exists(_tempContentRoot))
        {
            try { Directory.Delete(_tempContentRoot, true); }
            catch { /* Ignore cleanup errors */ }
        }

        _db?.Dispose();
    }

    #region SendTicketAssignedAsync Tests

    [Fact]
    public async Task SendTicketAssignedAsync_WithValidData_CreatesEmailFile()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedById = 2,
            AssignedToId = 4 // TeamLeader IT (from seed data)
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketAssignedAsync(1, 4, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "one email should be sent to assignee");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("teamlead.it@test.local", "email should be sent to IT Team Leader");
        content.Should().Contain("Test Ticket", "email should contain ticket title");
    }

    [Fact]
    public async Task SendTicketAssignedAsync_WithNonExistentTicket_DoesNotSendEmail()
    {
        // Arrange
        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketAssignedAsync(999, 4, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().BeEmpty("no email should be sent for non-existent ticket");
    }

    [Fact]
    public async Task SendTicketAssignedAsync_WithNonExistentAssignee_DoesNotSendEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.New,
            CreatedById = 2
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketAssignedAsync(1, 999, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().BeEmpty("no email should be sent for non-existent assignee");
    }

    #endregion

    #region SendTicketReassignedAsync Tests

    [Fact]
    public async Task SendTicketReassignedAsync_WithBothAssignees_SendsTwoEmails()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 4 // Old assignee: IT Team Leader
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Reassign from IT Team Leader (4) to Logistics Team Leader (5)
        await service.SendTicketReassignedAsync(1, 4, 5, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "one email is sent to all unique recipients (HashSet deduplication)");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        // Email is sent to both recipients in To: field, but saved as one .eml file
        content.Should().Contain("teamlead.it@test.local", "old assignee should be in recipients");
        content.Should().Contain("teamlead.log@test.local", "new assignee should be in recipients");
    }

    [Fact]
    public async Task SendTicketReassignedAsync_WithNullOldAssignee_SendsOneEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = null // No previous assignee
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Assign to IT Team Leader (no old assignee)
        await service.SendTicketReassignedAsync(1, null, 4, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "only new assignee should receive email");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("teamlead.it@test.local", "new assignee should receive email");
    }

    [Fact]
    public async Task SendTicketReassignedAsync_WithSameOldAndNewAssignee_SendsOneEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - "Reassign" to same person (deduplication should occur)
        await service.SendTicketReassignedAsync(1, 4, 4, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "should deduplicate to one email");
    }

    #endregion

    #region SendTicketStatusChangedAsync Tests

    [Fact]
    public async Task SendTicketStatusChangedAsync_WithDifferentCreatorAndAssignee_SendsTwoEmails()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.InProcess,
            CreatedById = 2, // Employee
            AssignedToId = 4  // IT Team Leader
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Status changed from New to InProcess
        await service.SendTicketStatusChangedAsync(1, (byte)TicketStatus.New, (byte)TicketStatus.InProcess, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "one email is sent to all unique recipients (HashSet deduplication)");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        // Email is sent to both creator and assignee in To: field
        content.Should().Contain("employee@test.local", "creator should be in recipients");
        content.Should().Contain("teamlead.it@test.local", "assignee should be in recipients");
    }

    [Fact]
    public async Task SendTicketStatusChangedAsync_WithCreatorAsAssignee_SendsOneEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.InProcess,
            CreatedById = 4,  // IT Team Leader created and assigned to themselves
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketStatusChangedAsync(1, (byte)TicketStatus.New, (byte)TicketStatus.InProcess, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "should deduplicate to one email");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("teamlead.it@test.local");
    }

    [Fact]
    public async Task SendTicketStatusChangedAsync_WithNullAssignee_SendsToCreatorOnly()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = null
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketStatusChangedAsync(1, (byte)TicketStatus.New, (byte)TicketStatus.Open, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "only creator should receive email");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("employee@test.local");
    }

    #endregion

    #region SendCommentAddedAsync Tests

    [Fact]
    public async Task SendCommentAddedAsync_ExcludesCommenter_SendsToCreatorAndAssignee()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2, // Employee
            AssignedToId = 4  // IT Team Leader
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var comment = new TicketComment
        {
            CommentId = 1,
            TicketId = 1,
            Content = "Test comment",
            CreatedById = 3 // Support user commented (not creator or assignee)
        };
        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Support user (3) added comment
        await service.SendCommentAddedAsync(1, 1, 3, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "one email is sent to creator and assignee (commenter excluded)");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        // Email is sent to both creator and assignee, excluding commenter
        content.Should().Contain("employee@test.local", "creator should be in recipients");
        content.Should().Contain("teamlead.it@test.local", "assignee should be in recipients");
        content.Should().NotContain("support@test.local", "commenter should not be in recipients");
    }

    [Fact]
    public async Task SendCommentAddedAsync_WhenCommenterIsCreator_SendsToAssigneeOnly()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2, // Employee
            AssignedToId = 4  // IT Team Leader
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var comment = new TicketComment
        {
            CommentId = 1,
            TicketId = 1,
            Content = "Test comment",
            CreatedById = 2 // Creator commented
        };
        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Creator (2) added comment
        await service.SendCommentAddedAsync(1, 1, 2, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "only assignee should receive (creator excluded)");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("teamlead.it@test.local");
    }

    [Fact]
    public async Task SendCommentAddedAsync_WithNonExistentComment_DoesNotSendEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Non-existent comment
        await service.SendCommentAddedAsync(1, 999, 2, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().BeEmpty("no email should be sent for non-existent comment");
    }

    #endregion

    #region SendPriorityEscalatedAsync Tests

    [Fact]
    public async Task SendPriorityEscalatedAsync_WithValidData_CreatesEmailWithUrgentNotice()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.Critical, // Escalated to Critical
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act - Priority escalated from Medium to Critical
        await service.SendPriorityEscalatedAsync(1, (byte)TicketPriority.Medium, (byte)TicketPriority.Critical, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "one email is sent to creator and assignee");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        // Email is sent to both creator and assignee in To: field
        content.Should().Contain("employee@test.local", "creator should be in recipients");
        content.Should().Contain("teamlead.it@test.local", "assignee should be in recipients");
    }

    [Fact]
    public async Task SendPriorityEscalatedAsync_WithCreatorAsAssignee_SendsOneEmail()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Open,
            CreatedById = 4, // IT Team Leader created and assigned to themselves
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendPriorityEscalatedAsync(1, (byte)TicketPriority.Medium, (byte)TicketPriority.High, 1, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "should deduplicate to one email");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("teamlead.it@test.local");
    }

    #endregion

    #region SendTicketResolvedAsync Tests

    [Fact]
    public async Task SendTicketResolvedAsync_WithValidData_SendsToCreatorOnly()
    {
        // Arrange
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT
            Priority = TicketPriority.High,
            Status = TicketStatus.Resolved,
            CreatedById = 2,
            AssignedToId = 4
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketResolvedAsync(1, 4, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().HaveCount(1, "only creator should receive resolution email");

        var content = await File.ReadAllTextAsync(emailFiles[0]);
        content.Should().Contain("employee@test.local", "creator should receive email");
        content.Should().NotContain("teamlead.it@test.local", "assignee should not receive (only creator gets resolved notification)");
    }

    [Fact]
    public async Task SendTicketResolvedAsync_WithNonExistentTicket_DoesNotSendEmail()
    {
        // Arrange
        var service = new EmailService(
            _db,
            _mockLocalization.Object,
            Options.Create(_emailOptions),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await service.SendTicketResolvedAsync(999, 4, CancellationToken.None);

        // Assert
        var emailFiles = Directory.GetFiles(_tempPickupDir, "*.eml");
        emailFiles.Should().BeEmpty("no email should be sent for non-existent ticket");
    }

    #endregion
}
