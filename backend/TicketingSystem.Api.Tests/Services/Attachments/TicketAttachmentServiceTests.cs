using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Tests.Helpers;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Tests.Services.Attachments;

public sealed class TicketAttachmentServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TicketAttachmentServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(AppDbContext db, string databaseName)> CreateContextAsync()
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString);
        var databaseName = $"ticketing_unit_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(adminBuilder.ConnectionString))
        {
            await admin.OpenAsync();
            await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", admin);
            await createCmd.ExecuteNonQueryAsync();
        }

        var tenantBuilder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = databaseName
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(tenantBuilder.ConnectionString)
            .Options;

        var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        return (db, databaseName);
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString);
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE);", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    private static TicketAttachmentService CreateService(AppDbContext db, Mock<IFileStorage>? storage = null)
    {
        if (storage == null)
        {
            storage = new Mock<IFileStorage>();
            storage.Setup(s => s.SaveAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<NpgsqlConnection?>(),
                    It.IsAny<NpgsqlTransaction?>()))
                .Returns(Task.CompletedTask);
            storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            storage.Setup(s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
        }

        var options = Options.Create(new FileUploadOptions
        {
            MaxFiles = 5,
            MaxFileSizeBytes = 5 * 1024 * 1024,
            MaxTotalSizeBytes = 10 * 1024 * 1024,
            AllowedContentTypes = new[] { "text/plain", "application/pdf" }
        });
        var validator = new AttachmentValidator(options);
        var history = new Mock<ITicketHistoryService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.GetUserId()).Returns(1);
        return new TicketAttachmentService(db, storage.Object, options, validator, history.Object, currentUser.Object);
    }

    private static async Task SeedTicketAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Name = "Uploader",
                    Email = "uploader@test.local",
                    PasswordHash = "hash",
                    RoleId = UserRole.Employee,
                    CategoryId = 1 // IT
                },
                new User
                {
                    Name = "Team Leader",
                    Email = "leader@test.local",
                    PasswordHash = "hash",
                    RoleId = UserRole.TeamLeader,
                    CategoryId = 1 // IT
                });
            await db.SaveChangesAsync();
        }

        if (!await db.Tickets.AnyAsync())
        {
            var uploader = await db.Users.FirstAsync();
            db.Tickets.Add(new Ticket
            {
                Title = "Ticket",
                Description = new string('x', 80),
                CategoryId = 1, // IT
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                CreatedById = uploader.UserId
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task AddAsync_WithValidFiles_SavesMetadataAndInvokesStorage()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();
            var uploaderId = await db.Users.Select(u => u.UserId).FirstAsync();

            var storage = new Mock<IFileStorage>();
            storage.Setup(s => s.SaveAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<NpgsqlConnection?>(),
                    It.IsAny<NpgsqlTransaction?>()))
                .Returns(Task.CompletedTask);
            storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateService(db, storage);
            var files = TestDataFactory.CreateFormFiles(2).ToArray();

            var result = await service.AddAsync(ticketId, files, uploaderId, CancellationToken.None);

            result.Should().HaveCount(2);
            (await db.TicketFiles.CountAsync()).Should().Be(2);
            storage.Verify(s => s.SaveAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<NpgsqlConnection?>(),
                It.IsAny<NpgsqlTransaction?>()), Times.Exactly(2));
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task AddAsync_WhenTicketMissing_ThrowsAppException()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var storage = new Mock<IFileStorage>();
            var service = CreateService(db, storage);
            var files = TestDataFactory.CreateFormFiles(1).ToArray();

            Func<Task> act = async () => await service.AddAsync(9999, files, 1, CancellationToken.None);

            await act.Should().ThrowAsync<AppException>()
                .Where(ex => ex.Code == ErrorCodes.TicketNotFound);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task ListAsync_WhenTicketNotFound_ThrowsAppException()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            var service = CreateService(db);

            Func<Task> act = async () => await service.ListAsync(7777, CancellationToken.None);

            await act.Should().ThrowAsync<AppException>()
                .Where(ex => ex.Code == ErrorCodes.TicketNotFound);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task AddAsync_WhenStorageThrows_RollsBackChanges()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();
            var uploaderId = await db.Users.Select(u => u.UserId).FirstAsync();

            var storage = new Mock<IFileStorage>();
            storage.SetupSequence(s => s.SaveAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<NpgsqlConnection?>(),
                    It.IsAny<NpgsqlTransaction?>()))
                .Returns(Task.CompletedTask)
                .ThrowsAsync(new InvalidOperationException("storage failure"));
            storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateService(db, storage);
            var files = TestDataFactory.CreateFormFiles(2).ToArray();

            Func<Task> act = async () => await service.AddAsync(ticketId, files, uploaderId, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            (await db.TicketFiles.CountAsync()).Should().Be(0);
            storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task ListAsync_ReturnsFilesNewestFirst()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();
            var uploaderId = await db.Users.Select(u => u.UserId).FirstAsync();

            db.TicketFiles.AddRange(
                new TicketFile
                {
                    TicketId = ticketId,
                    OriginalName = "older.txt",
                    StoredName = "older",
                    ContentType = "text/plain",
                    SizeBytes = 10,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                    UploaderUserId = uploaderId
                },
                new TicketFile
                {
                    TicketId = ticketId,
                    OriginalName = "newer.txt",
                    StoredName = "newer",
                    ContentType = "text/plain",
                    SizeBytes = 12,
                    CreatedAt = DateTime.UtcNow,
                    UploaderUserId = uploaderId
                });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var list = await service.ListAsync(ticketId, CancellationToken.None);

            list.Should().HaveCount(2);
            list.First().OriginalName.Should().Be("newer.txt");
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task OpenForDownloadAsync_ReturnsStream()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();
            var uploaderId = await db.Users.Select(u => u.UserId).FirstAsync();

            var file = new TicketFile
            {
                TicketId = ticketId,
                OriginalName = "download.txt",
                StoredName = "stored",
                ContentType = "text/plain",
                SizeBytes = 5,
                CreatedAt = DateTime.UtcNow,
                UploaderUserId = uploaderId
            };
            db.TicketFiles.Add(file);
            await db.SaveChangesAsync();

            var storage = new Mock<IFileStorage>();
            storage.Setup(s => s.OpenReadAsync($"db:{file.TicketFileId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(new byte[] { 10, 20 }));
            storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<NpgsqlConnection?>(), It.IsAny<NpgsqlTransaction?>()))
                .Returns(Task.CompletedTask);
            storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var service = CreateService(db, storage);
            var download = await service.OpenForDownloadAsync(ticketId, file.TicketFileId, CancellationToken.None);

            download.ContentType.Should().Be("text/plain");
            download.OriginalName.Should().Be("download.txt");
            using var reader = new StreamReader(download.Content);
            (await reader.ReadToEndAsync()).Should().NotBeEmpty();
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task OpenForDownloadAsync_NonExistingFile_ThrowsAppException()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();

            var service = CreateService(db);

            Func<Task> act = async () => await service.OpenForDownloadAsync(ticketId, 9999, CancellationToken.None);

            await act.Should().ThrowAsync<AppException>()
                .Where(ex => ex.Code == ErrorCodes.FileNotFound);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task DeleteAsync_RemovesMetadataAndInvokesStorage()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var ticketId = await db.Tickets.Select(t => t.TicketId).FirstAsync();
            var uploaderId = await db.Users.Select(u => u.UserId).FirstAsync();

            var file = new TicketFile
            {
                TicketId = ticketId,
                OriginalName = "delete.txt",
                StoredName = "stored",
                ContentType = "text/plain",
                SizeBytes = 20,
                CreatedAt = DateTime.UtcNow,
                UploaderUserId = uploaderId
            };
            db.TicketFiles.Add(file);
            await db.SaveChangesAsync();

            var storage = new Mock<IFileStorage>();
            storage.Setup(s => s.DeleteAsync($"db:{file.TicketFileId}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            storage.Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<NpgsqlConnection?>(), It.IsAny<NpgsqlTransaction?>()))
                .Returns(Task.CompletedTask);

            var service = CreateService(db, storage);
            var result = await service.DeleteAsync(file.TicketFileId, CancellationToken.None);

            result.Should().BeTrue();
            (await db.TicketFiles.AnyAsync(tf => tf.TicketFileId == file.TicketFileId)).Should().BeFalse();
            storage.Verify(s => s.DeleteAsync($"db:{file.TicketFileId}", It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_ThrowsAppException()
    {
        var (db, databaseName) = await CreateContextAsync();
        try
        {
            await SeedTicketAsync(db);
            var service = CreateService(db);

            Func<Task> act = async () => await service.DeleteAsync(9999, CancellationToken.None);

            await act.Should().ThrowAsync<AppException>()
                .Where(ex => ex.Code == ErrorCodes.FileNotFound);
        }
        finally
        {
            await db.DisposeAsync();
            await DropDatabaseAsync(databaseName);
        }
    }
}

