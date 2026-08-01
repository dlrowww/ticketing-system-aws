using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketingSystem.Api.Data;
using TicketingSystem.Api.DTOs;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.IntegrationTests.Helpers;
using TicketingSystem.Api.Models;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public class CategoriesControllerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres;
    private TestWebApplicationFactory? _factory;
    private HttpClient? _client;

    public CategoriesControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        _factory = await TestWebApplicationFactory.CreateAsync(_postgres.ConnectionString);
        _client = _factory.CreateClient();

        // Seed database with test data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // The database migrations already seed categories 1, 2, 3
        // Just add one inactive category for testing
        if (!await db.Categories.AnyAsync(c => c.CategoryId == 4))
        {
            db.Categories.Add(new Category
            {
                CategoryId = 4,
                NamePl = "Test Inactive",
                NameEn = "Test Inactive",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            
            // Update the sequence to avoid conflicts when auto-generating CategoryId
            await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('\"Categories\"', 'CategoryId'), 4, true);");
        }

        // Add admin user
        db.Users.Add(new User
        {
            UserId = 999,
            Name = "Admin Test",
            Email = "admin@test.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            RoleId = UserRole.Admin,
            CategoryId = null
        });

        // Add non-admin user
        db.Users.Add(new User
        {
            UserId = 1000,
            Name = "Employee Test",
            Email = "employee@test.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("employee"),
            RoleId = UserRole.Employee,
            CategoryId = 1
        });

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "admin@test.local",
            Password = "admin"
        };

        var response = await _client!.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.Should().BeSuccessful();

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var token = jsonDoc!.RootElement.GetProperty("token").GetString();
        return token!;
    }

    #region GET /api/categories Tests

    [Fact]
    public async Task GetCategories_WithoutAuth_Returns200AndActiveCategories()
    {
        // Act
        var response = await _client!.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        categories.Should().NotBeNull();
        categories.Should().HaveCountGreaterOrEqualTo(3); // At least IT, Logistics, Administration
        categories.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task GetCategories_WithIncludeInactiveTrue_ReturnsAllCategories()
    {
        // Act
        var response = await _client!.GetAsync("/api/categories?includeInactive=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();

        categories.Should().NotBeNull();
        categories.Should().HaveCountGreaterOrEqualTo(4); // Active + Test Inactive
        categories.Should().Contain(c => !c.IsActive); // At least one inactive
    }

    [Fact]
    public async Task GetCategories_WithEmptyDatabase_ReturnsEmptyArray()
    {
        // Arrange - Clear all categories (must delete referencing data first due to FK constraints)
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Delete tickets first (references categories)
        db.Tickets.RemoveRange(db.Tickets);
        await db.SaveChangesAsync();
        
        // Delete users that reference categories
        var usersWithCategories = await db.Users.Where(u => u.CategoryId != null).ToListAsync();
        foreach (var user in usersWithCategories)
        {
            user.CategoryId = null;  // Clear FK first
        }
        await db.SaveChangesAsync();
        
        // Now safe to delete categories
        db.Categories.RemoveRange(db.Categories);
        await db.SaveChangesAsync();

        // Act
        var response = await _client!.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().NotBeNull();
        categories.Should().BeEmpty();
    }

    #endregion

    #region GET /api/categories/{id} Tests

    [Fact]
    public async Task GetCategoryById_WithExistingId_Returns200AndCategory()
    {
        // Act
        var response = await _client!.GetAsync("/api/categories/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();

        category.Should().NotBeNull();
        category!.CategoryId.Should().Be(1);
        category.NamePl.Should().Be("IT");
        category.NameEn.Should().Be("IT");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoryById_WithNonExistingId_Returns404()
    {
        // Act
        var response = await _client!.GetAsync("/api/categories/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/categories Tests

    [Fact]
    public async Task CreateCategory_AsAdmin_Returns201AndCreatedCategory()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        var authorizedClient = _factory!.CreateClient();
        authorizedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8]; // Use first 8 chars of GUID
        var request = new CreateCategoryRequest
        {
            NamePl = $"Test PL {uniqueSuffix}",
            NameEn = $"Test EN {uniqueSuffix}"
        };

        // Act
        var response = await authorizedClient.PostAsJsonAsync("/api/categories", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"CreateCategory failed: {response.StatusCode} - {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();

        category.Should().NotBeNull();
        category!.CategoryId.Should().BeGreaterThan(0);
        category.NamePl.Should().Be(request.NamePl);
        category.NameEn.Should().Be(request.NameEn);
        category.IsActive.Should().BeTrue();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().ToLowerInvariant().Should().Contain($"/api/categories/{category.CategoryId}");
    }

    [Fact]
    public async Task CreateCategory_AsNonAdmin_Returns403Forbidden()
    {
        // Arrange - Login as employee
        var loginRequest = new LoginRequest
        {
            Email = "employee@test.local",
            Password = "employee"
        };

        var loginResponse = await _client!.PostAsJsonAsync("/api/auth/login", loginRequest);
        var jsonDoc = await loginResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var token = jsonDoc!.RootElement.GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCategoryRequest
        {
            NamePl = "Finanse",
            NameEn = "Finance"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_Returns409Conflict()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCategoryRequest
        {
            NamePl = "IT", // Duplicate Polish name
            NameEn = "New IT"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region PATCH /api/categories/{id} Tests

    [Fact]
    public async Task UpdateCategory_AsAdmin_Returns200AndUpdatedCategory()
    {
        // Arrange
        var token = await GetAdminTokenAsync();
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateCategoryRequest
        {
            NamePl = "IT Support",
            NameEn = "IT Support",
            IsActive = true
        };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/categories/1", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();

        category.Should().NotBeNull();
        category!.CategoryId.Should().Be(1);
        category.NamePl.Should().Be("IT Support");
        category.NameEn.Should().Be("IT Support");
        category.IsActive.Should().BeTrue();
    }

    #endregion

    #region DELETE /api/categories/{id} Tests

    [Fact]
    public async Task DeleteCategory_WithUnusedCategory_Returns204NoContent()
    {
        // Arrange - Create new category without any tickets/users
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var newCategory = new Category
        {
            CategoryId = 100, // Use explicit high ID to avoid conflicts
            NamePl = "Test Deletable Category",
            NameEn = "Test Deletable Category",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Categories.Add(newCategory);
        await db.SaveChangesAsync();
        var categoryId = newCategory.CategoryId;

        var token = await GetAdminTokenAsync();
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/categories/{categoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify category is deleted
        var getResponse = await _client.GetAsync($"/api/categories/{categoryId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
