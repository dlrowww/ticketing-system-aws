using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Tickets;
using TicketingSystem.Api.Tests.Helpers;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.Tickets;

public class TicketAssignmentValidatorTests
{
    [Fact]
    public async Task ValidateAssignmentAsync_WithValidSupport_DoesNotThrow()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var supportUser = new User
        {
            UserId = 10,
            Name = "Support User",
            Email = "support@test.com",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1, // IT category
            IsActive = true
        };
        db.Users.Add(supportUser);
        await db.SaveChangesAsync();

        var validator = new TicketAssignmentValidator(db);

        // Act
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 1, userId: 10, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithValidTeamLeader_DoesNotThrow()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var teamLeader = new User
        {
            UserId = 11,
            Name = "Team Leader",
            Email = "leader@test.com",
            PasswordHash = "hash",
            RoleId = UserRole.TeamLeader,
            CategoryId = 2, // Logistics category
            IsActive = true
        };
        db.Users.Add(teamLeader);
        await db.SaveChangesAsync();

        var validator = new TicketAssignmentValidator(db);

        // Act
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 2, userId: 11, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithValidAdmin_DoesNotThrow()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        // Admin user (ID=1) is already seeded in test data
        var validator = new TicketAssignmentValidator(db);

        // Act - Admin can be assigned to any category
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 1, userId: 1, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithNonExistentUser_ThrowsUserNotFound()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var validator = new TicketAssignmentValidator(db);

        // Act
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 1, userId: 9999, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.UserNotFound && ex.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithInactiveUser_ThrowsUserInactive()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var inactiveUser = new User
        {
            UserId = 12,
            Name = "Inactive User",
            Email = "inactive@test.com",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1,
            IsActive = false // Inactive
        };
        db.Users.Add(inactiveUser);
        await db.SaveChangesAsync();

        var validator = new TicketAssignmentValidator(db);

        // Act
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 1, userId: 12, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.UserInactive && ex.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithEmployeeRole_ThrowsInvalidAssigneeRole()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        // Employee user (ID=2) is already seeded with Employee role
        var validator = new TicketAssignmentValidator(db);

        // Act
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 1, userId: 2, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.InvalidAssigneeRole && ex.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithSupportCategoryMismatch_ThrowsAssigneeCategoryMismatch()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var supportUser = new User
        {
            UserId = 13,
            Name = "IT Support",
            Email = "itsupport@test.com",
            PasswordHash = "hash",
            RoleId = UserRole.Support,
            CategoryId = 1, // IT category
            IsActive = true
        };
        db.Users.Add(supportUser);
        await db.SaveChangesAsync();

        var validator = new TicketAssignmentValidator(db);

        // Act - Try to assign IT Support to Logistics ticket (category 2)
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 2, userId: 13, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.AssigneeCategoryMismatch && ex.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateAssignmentAsync_WithAdminInDifferentCategory_DoesNotThrow()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var adminUser = new User
        {
            UserId = 14,
            Name = "IT Admin",
            Email = "itadmin@test.com",
            PasswordHash = "hash",
            RoleId = UserRole.Admin,
            CategoryId = 1, // IT category
            IsActive = true
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var validator = new TicketAssignmentValidator(db);

        // Act - Admin can be assigned to any category (Logistics in this case)
        Func<Task> act = async () => await validator.ValidateAssignmentAsync(ticketCategoryId: 2, userId: 14, CancellationToken.None);

        // Assert - Should NOT throw (Admin is exempt from category check)
        await act.Should().NotThrowAsync();
    }
}
