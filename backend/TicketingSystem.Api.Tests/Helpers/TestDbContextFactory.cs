using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using TicketingSystem.Api.Data;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Models;

namespace TicketingSystem.Api.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<AppDbContext> CreateWithSeedDataAsync(string? dbName = null)
    {
        var context = CreateInMemory(dbName);
        await SeedUsersAsync(context);
        return context;
    }

    public static async Task<(AppDbContext Db, SqliteConnection Connection)> CreateSqliteWithSeedDataAsync()
    {
        var context = CreateSqliteInMemory(out var connection);
        await SeedUsersAsync(context);
        return (context, connection);
    }

    public static AppDbContext CreateSqliteInMemory(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        // Seed categories first (required for FK constraints)
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { CategoryId = 3, NamePl = "Administracja", NameEn = "Administration", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        context.Users.AddRange(
            new User { UserId = 1, Name = "Test Admin", Email = "admin@test.local", PasswordHash = "hash", RoleId = UserRole.Admin },
            new User { UserId = 2, Name = "Employee One", Email = "employee@test.local", PasswordHash = "hash", RoleId = UserRole.Employee, CategoryId = 1 },
            new User { UserId = 3, Name = "Support One", Email = "support@test.local", PasswordHash = "hash", RoleId = UserRole.Support, CategoryId = 1 },
            new User { UserId = 4, Name = "TeamLeader IT", Email = "teamlead.it@test.local", PasswordHash = "hash", RoleId = UserRole.TeamLeader, CategoryId = 1 },
            new User { UserId = 5, Name = "TeamLeader Logistics", Email = "teamlead.log@test.local", PasswordHash = "hash", RoleId = UserRole.TeamLeader, CategoryId = 2 }
        );
        await context.SaveChangesAsync();
    }
}
