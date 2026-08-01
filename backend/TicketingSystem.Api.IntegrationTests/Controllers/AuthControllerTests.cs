using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TicketingSystem.Api.DTOs;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.IntegrationTests.Helpers;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class AuthControllerTests
{
    private readonly PostgresTestContainer _postgres;

    public AuthControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsCookieWithJwt()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);
        const string email = "admin@example.com";
        const string password = "Admin#123";

        await SeedUserAsync(ctx, "Test User", email, password, UserRole.Admin);

        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("auth_token="));

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        payload.Should().NotBeNull();
        payload!["code"].Should().Be("SUCCESS");
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);
        const string password = "Admin#123";
        await SeedUserAsync(ctx, "Test User", "admin@example.com", password, UserRole.Admin);

        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = "missing@example.com", Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProblemCodeAsync(response, "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);
        const string email = "admin@example.com";
        await SeedUserAsync(ctx, "Test User", email, "Admin#123", UserRole.Admin);

        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "WrongPass!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProblemCodeAsync(response, "INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Logout_ClearsCookie()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);
        const string email = "employee@example.com";
        const string password = "Employee#123";

        await SeedUserAsync(ctx, "Employee", email, password, UserRole.Employee, 1); // IT
        await AuthenticateAsync(ctx, email, password);

        var response = await ctx.Client.PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var logoutCookie = cookies!.FirstOrDefault(c => c.StartsWith("auth_token=", StringComparison.Ordinal));
        logoutCookie.Should().NotBeNull();
        logoutCookie!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First().Should().Be("auth_token=");
        logoutCookie.Should().Contain("expires=");
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);
        const string email = "leader@example.com";
        const string password = "Leader#123";

        var userId = await SeedUserAsync(ctx, "Team Leader", email, password, UserRole.TeamLeader, 2); // Logistics
        await AuthenticateAsync(ctx, email, password);

        var response = await ctx.Client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        current.Should().NotBeNull();
        current!.UserId.Should().Be(userId);
        current.Name.Should().Be("Team Leader");
        current.Email.Should().Be(email);
        current.RoleId.Should().Be((byte)UserRole.TeamLeader);
        current.CategoryId.Should().Be((byte)2); // Logistics
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var response = await ctx.Client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<int> SeedUserAsync(IntegrationTestContext ctx, string name, string email, string password, UserRole role, int? category = null)
    {
        return await ctx.WithDbContextAsync(async db =>
        {
            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                RoleId = role,
                CategoryId = category
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.UserId;
        });
    }

    private static async Task<AuthSession> AuthenticateAsync(IntegrationTestContext ctx, string email, string password)
    {
        var response = await ctx.Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var authCookie = cookies!.First(c => c.StartsWith("auth_token=", StringComparison.Ordinal));
        var token = authCookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .First()
            .Substring("auth_token=".Length);

        ctx.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return new AuthSession(token, authCookie);
    }

    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().Be(expectedCode);
    }

    private sealed record AuthSession(string Token, string RawCookie);
}



