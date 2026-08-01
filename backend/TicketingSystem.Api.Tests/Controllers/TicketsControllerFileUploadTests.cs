using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TicketingSystem.Api.Controllers;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Tests.Helpers;
using Xunit;

namespace TicketingSystem.Api.Tests.Controllers;

/// <summary>
/// Unit tests for file upload functionality in TicketsController.
/// Tests the AddFiles endpoint which handles multipart/form-data file uploads.
/// </summary>
public class TicketsControllerFileUploadTests
{
    [Fact]
    public async Task AddFiles_WithValidFiles_ReturnsUploadedFiles()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        
        // Create a test ticket
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = (int)TicketCategory.IT,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Returns(2);

        var mockAttachmentService = new Mock<ITicketAttachmentService>();
        var expectedFiles = new List<TicketFileDto>
        {
            new(1, 1, "test.txt", "text/plain", 1024, null, "checksum123", "2024-01-01T00:00:00Z", 2)
        };
        mockAttachmentService.Setup(x => x.AddAsync(
            It.IsAny<int>(),
            It.IsAny<IFormFile[]>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>(),
            null,
            null))
            .ReturnsAsync(expectedFiles);

        var mockHistoryService = new Mock<ITicketHistoryService>();

        var controller = new TicketsController(
            Mock.Of<ITicketService>(),
            db,
            mockAttachmentService.Object,
            Mock.Of<ICommentService>(),
            mockHistoryService.Object,
            mockCurrentUser.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.txt");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("text/plain");

        // Act
        var result = await controller.AddFiles(1, new[] { mockFile.Object }, CancellationToken.None);

        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var files = okResult!.Value as IReadOnlyList<TicketFileDto>;
        files.Should().NotBeNull();
        files.Should().HaveCount(1);
        files!.First().OriginalName.Should().Be("test.txt");
        
        mockAttachmentService.Verify(x => x.AddAsync(
            1,
            It.Is<IFormFile[]>(files => files.Length == 1),
            2,
            CancellationToken.None,
            null,
            null),
            Times.Once);
    }

    [Fact]
    public async Task AddFiles_WithNonExistentTicket_ThrowsAppException()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Returns(2);

        var controller = new TicketsController(
            Mock.Of<ITicketService>(),
            db,
            Mock.Of<ITicketAttachmentService>(),
            Mock.Of<ICommentService>(),
            Mock.Of<ITicketHistoryService>(),
            mockCurrentUser.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.txt");

        // Act
        Func<Task> act = async () => await controller.AddFiles(999, new[] { mockFile.Object }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Common.AppException>()
            .Where(ex => ex.Code == Common.ErrorCodes.TicketNotFound);
    }

    [Fact]
    public async Task AddFiles_WithUnauthenticatedUser_ThrowsAppException()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = (int)TicketCategory.IT,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Throws(new Common.AppException(
            Common.ErrorCodes.Unauthenticated,
            "Authentication required.",
            System.Net.HttpStatusCode.Unauthorized));

        var controller = new TicketsController(
            Mock.Of<ITicketService>(),
            db,
            Mock.Of<ITicketAttachmentService>(),
            Mock.Of<ICommentService>(),
            Mock.Of<ITicketHistoryService>(),
            mockCurrentUser.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.txt");

        // Act
        Func<Task> act = async () => await controller.AddFiles(1, new[] { mockFile.Object }, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Common.AppException>()
            .Where(ex => ex.Code == Common.ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task AddFiles_WithEmptyFileArray_CallsServiceWithEmptyArray()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = (int)TicketCategory.IT,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Returns(2);

        var mockAttachmentService = new Mock<ITicketAttachmentService>();
        mockAttachmentService.Setup(x => x.AddAsync(
            It.IsAny<int>(),
            It.IsAny<IFormFile[]>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>(),
            null,
            null))
            .ReturnsAsync(new List<TicketFileDto>());

        var controller = new TicketsController(
            Mock.Of<ITicketService>(),
            db,
            mockAttachmentService.Object,
            Mock.Of<ICommentService>(),
            Mock.Of<ITicketHistoryService>(),
            mockCurrentUser.Object);

        // Act
        var result = await controller.AddFiles(1, Array.Empty<IFormFile>(), CancellationToken.None);

        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var files = okResult!.Value as IReadOnlyList<TicketFileDto>;
        files.Should().NotBeNull();
        files.Should().BeEmpty();
        
        mockAttachmentService.Verify(x => x.AddAsync(
            1,
            It.Is<IFormFile[]>(files => files.Length == 0),
            2,
            CancellationToken.None,
            null,
            null),
            Times.Once);
    }

    [Fact]
    public async Task AddFiles_WithMultipleFiles_ReturnsAllUploadedFiles()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        
        var ticket = new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = (int)TicketCategory.IT,
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedById = 2,
            CreatedAt = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Returns(2);

        var mockAttachmentService = new Mock<ITicketAttachmentService>();
        var expectedFiles = new List<TicketFileDto>
        {
            new(1, 1, "file1.txt", "text/plain", 1024, null, "checksum1", "2024-01-01T00:00:00Z", 2),
            new(2, 1, "file2.pdf", "application/pdf", 2048, null, "checksum2", "2024-01-01T00:00:00Z", 2)
        };
        mockAttachmentService.Setup(x => x.AddAsync(
            It.IsAny<int>(),
            It.IsAny<IFormFile[]>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>(),
            null,
            null))
            .ReturnsAsync(expectedFiles);

        var controller = new TicketsController(
            Mock.Of<ITicketService>(),
            db,
            mockAttachmentService.Object,
            Mock.Of<ICommentService>(),
            Mock.Of<ITicketHistoryService>(),
            mockCurrentUser.Object);

        var mockFile1 = new Mock<IFormFile>();
        mockFile1.Setup(f => f.FileName).Returns("file1.txt");
        mockFile1.Setup(f => f.Length).Returns(1024);

        var mockFile2 = new Mock<IFormFile>();
        mockFile2.Setup(f => f.FileName).Returns("file2.pdf");
        mockFile2.Setup(f => f.Length).Returns(2048);

        // Act
        var result = await controller.AddFiles(1, new[] { mockFile1.Object, mockFile2.Object }, CancellationToken.None);

        // Assert
        var okResult = result.Result as Microsoft.AspNetCore.Mvc.OkObjectResult;
        okResult.Should().NotBeNull();
        var files = okResult!.Value as IReadOnlyList<TicketFileDto>;
        files.Should().NotBeNull();
        files.Should().HaveCount(2);
        files![0].OriginalName.Should().Be("file1.txt");
        files[1].OriginalName.Should().Be("file2.pdf");
    }
}
