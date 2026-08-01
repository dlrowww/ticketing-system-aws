using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.IntegrationTests.Helpers;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class UsersControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgresTestContainer _postgres;

    public UsersControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var response = await ctx.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithNonAdmin_Returns403()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var employeeEmail = "employee@it.local";
        var employeePassword = "Employee123!";
        await SeedUserAsync(ctx, "Employee", employeeEmail, employeePassword, UserRole.Employee, 1); // IT

        await AuthenticateAsync(ctx, employeeEmail, employeePassword);

        var response = await ctx.Client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_CreatesAndReturnsDetails()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var adminEmail = "admin@it.local";
        var adminPassword = "Admin123!";
        await SeedUserAsync(ctx, "Admin", adminEmail, adminPassword, UserRole.Admin);

        await AuthenticateAsync(ctx, adminEmail, adminPassword);

        var create = new CreateUserRequest
        {
            Name = "Support One",
            Email = "support.one@it.local",
            Password = "Support123!",
            Role = (byte)UserRole.Support,
            CategoryId = (byte)TicketCategory.IT
        };

        var response = await ctx.Client.PostAsJsonAsync("/api/users", create);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.UserId.Should().BeGreaterThan(0);
        body.Email.Should().Be("support.one@it.local");
        body.Role.Should().Be((byte)UserRole.Support);
        body.CategoryId.Should().Be((byte)1); // IT
        body.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_SetsInactive()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var adminEmail = "admin@it.local";
        var adminPassword = "Admin123!";
        await SeedUserAsync(ctx, "Admin", adminEmail, adminPassword, UserRole.Admin);

        var targetId = await SeedUserAsync(ctx, "Target", "target@it.local", "Target123!", UserRole.Employee, 1); // IT

        await AuthenticateAsync(ctx, adminEmail, adminPassword);

        var deleteResponse = await ctx.Client.DeleteAsync($"/api/users/{targetId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await ctx.Client.GetAsync($"/api/users/{targetId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<UserDetailsDto>(JsonOptions);
        body!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns401WithUserInactiveCode()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var email = "inactive@it.local";
        var password = "Inactive123!";
        var id = await SeedUserAsync(ctx, "Inactive", email, password, UserRole.Employee, 1); // IT

        await ctx.WithDbContextAsync(async db =>
        {
            var u = await db.Users.FindAsync(id);
            u!.IsActive = false;
            await db.SaveChangesAsync();
        });

        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain(ErrorCodes.UserInactive);
    }

    private static async Task<int> SeedUserAsync(
        IntegrationTestContext ctx,
        string name,
        string email,
        string password,
        UserRole role,
        int? category = null)
    {
        return await ctx.WithDbContextAsync(async db =>
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                RoleId = role,
                CategoryId = category,
                IsActive = true
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.UserId;
        });
    }

    private static async Task AuthenticateAsync(IntegrationTestContext ctx, string email, string password)
    {
        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var authCookie = cookies!.First(c => c.StartsWith("auth_token=", StringComparison.Ordinal));
        var token = authCookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First()
            .Substring("auth_token=".Length);

        ctx.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}



