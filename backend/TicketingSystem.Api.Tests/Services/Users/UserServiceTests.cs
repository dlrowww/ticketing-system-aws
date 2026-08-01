using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using Moq;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Services.Users.Admin;
using TicketingSystem.Api.Tests.Helpers;
using TicketingSystem.Api.Validators.Users;

namespace TicketingSystem.Api.Tests.Services.Users;

public sealed class UserServiceTests
{
    private static UserService CreateService(
        TicketingSystem.Api.Data.AppDbContext db,
        Mock<ICurrentUserService>? currentUser = null,
        IUserValidator? validator = null)
    {
        currentUser ??= new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1);

        validator ??= new UserValidator();

        return new UserService(db, validator, currentUser.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesUser()
    {
        await using var db = TestDbContextFactory.CreateInMemory();
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateUserRequest
        {
            Name = "John Doe",
            Email = "JOHN.DOE@EXAMPLE.COM",
            Password = "Password123!",
            Role = (byte)UserRole.Employee,
            CategoryId = null
        }, CancellationToken.None);

        created.UserId.Should().BeGreaterThan(0);
        created.Email.Should().Be("john.doe@example.com");
        created.IsActive.Should().BeTrue();

        var fromDb = await db.Users.FindAsync(created.UserId);
        fromDb.Should().NotBeNull();
        fromDb!.PasswordHash.Should().NotBeNullOrWhiteSpace();
        fromDb.PasswordHash.Should().NotBe("Password123!");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        Func<Task> act = async () => await service.CreateAsync(new CreateUserRequest
        {
            Name = "Dup",
            Email = "ADMIN@TEST.LOCAL",
            Password = "Password123!",
            Role = (byte)UserRole.Employee
        }, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserEmailAlreadyExists && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ThrowsNotFound()
    {
        await using var db = TestDbContextFactory.CreateInMemory();
        var service = CreateService(db);

        Func<Task> act = async () => await service.UpdateAsync(999, new UpdateUserRequest { Name = "X" }, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserNotFound && e.Status == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateSelf_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1); // Test Admin seeded with UserId=1

        var service = CreateService(db, currentUser);

        Func<Task> act = async () => await service.UpdateAsync(1, new UpdateUserRequest { IsActive = false }, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserCannotDeactivateSelf && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WithEmailChangeToExisting_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Update user 2 (Employee One) to email of user 3 (support@test.local)
        Func<Task> act = async () => await service.UpdateAsync(2, new UpdateUserRequest { Email = "support@test.local" }, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserEmailAlreadyExists && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WithValidFields_UpdatesUser()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        var updated = await service.UpdateAsync(2, new UpdateUserRequest
        {
            Name = "Employee Two",
            Email = "employee2@test.local",
            Role = (byte)UserRole.Support,
            CategoryId = (byte)TicketCategory.Logistics,
            IsActive = true
        }, CancellationToken.None);

        updated.Name.Should().Be("Employee Two");
        updated.Email.Should().Be("employee2@test.local");
        updated.Role.Should().Be((byte)UserRole.Support);
        updated.CategoryId.Should().Be((byte)TicketCategory.Logistics);
        updated.IsActive.Should().BeTrue();

        var fromDb = await db.Users.FindAsync(2);
        fromDb!.Name.Should().Be("Employee Two");
        fromDb.Email.Should().Be("employee2@test.local");
        fromDb.RoleId.Should().Be(UserRole.Support);
        fromDb.CategoryId.Should().Be(2); // Logistics
    }

    [Fact]
    public async Task DeleteAsync_SetsInactive()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        await service.DeleteAsync(2, CancellationToken.None);

        var fromDb = await db.Users.FindAsync(2);
        fromDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_SelfDelete_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns(1);
        var service = CreateService(db, currentUser);

        Func<Task> act = async () => await service.DeleteAsync(1, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserCannotDeactivateSelf && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateUserWithAssignedTickets_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Create an open ticket assigned to user 3
        db.Tickets.Add(new TicketingSystem.Api.Models.Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT category
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Open,
            CreatedById = 2,
            AssignedToId = 3,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Try to deactivate user 3 who has assigned tickets
        Func<Task> act = async () => await service.UpdateAsync(3, new UpdateUserRequest { IsActive = false }, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserHasAssignedTickets && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateUserWithResolvedTicketsOnly_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Create a resolved ticket assigned to user 3
        db.Tickets.Add(new TicketingSystem.Api.Models.Ticket
        {
            Title = "Resolved Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT category
            Priority = TicketPriority.Medium,
            Status = TicketStatus.Resolved,
            CreatedById = 2,
            AssignedToId = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Deactivating user 3 should succeed because resolved tickets don't block
        var updated = await service.UpdateAsync(3, new UpdateUserRequest { IsActive = false }, CancellationToken.None);

        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_UserWithAssignedTickets_ThrowsConflict()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Create an in-progress ticket assigned to user 3
        db.Tickets.Add(new TicketingSystem.Api.Models.Ticket
        {
            Title = "In Progress Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT category
            Priority = TicketPriority.High,
            Status = TicketStatus.InProcess,
            CreatedById = 2,
            AssignedToId = 3,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Try to delete user 3 who has assigned tickets
        Func<Task> act = async () => await service.DeleteAsync(3, CancellationToken.None);

        await act.Should().ThrowAsync<AppException>()
            .Where(e => e.Code == ErrorCodes.UserHasAssignedTickets && e.Status == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAsync_UserWithCanceledTicketsOnly_Succeeds()
    {
        await using var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var service = CreateService(db);

        // Create a canceled ticket assigned to user 3
        db.Tickets.Add(new TicketingSystem.Api.Models.Ticket
        {
            Title = "Canceled Ticket",
            Description = "Test Description",
            CategoryId = 1, // IT category
            Priority = TicketPriority.Low,
            Status = TicketStatus.Cancelled,
            CreatedById = 2,
            AssignedToId = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Deleting user 3 should succeed because canceled tickets don't block
        await service.DeleteAsync(3, CancellationToken.None);

        var fromDb = await db.Users.FindAsync(3);
        fromDb!.IsActive.Should().BeFalse();
    }
}
