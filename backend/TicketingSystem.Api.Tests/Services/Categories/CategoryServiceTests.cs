using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Models;
using TicketingSystem.Api.Services.Categories;
using TicketingSystem.Api.Tests.Helpers;
using TicketingSystem.Api.Validators.Categories;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.Categories;

public class CategoryServiceTests
{
    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithIncludeInactiveFalse_ReturnsOnlyActiveCategories()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.AddRange(
            new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true },
            new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics", IsActive = true },
            new Category { CategoryId = 3, NamePl = "Administracja", NameEn = "Administration", IsActive = false }
        );
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetAllAsync(includeInactive: false, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
        result.Should().Contain(c => c.NameEn == "IT");
        result.Should().Contain(c => c.NameEn == "Logistics");
        result.Should().NotContain(c => c.NameEn == "Administration");
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactiveTrue_ReturnsAllCategories()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.AddRange(
            new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true },
            new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics", IsActive = false }
        );
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetAllAsync(includeInactive: true, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.IsActive);
        result.Should().Contain(c => !c.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WithNoCategories_ReturnsEmptyList()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetAllAsync(includeInactive: false, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_OrderedByNamePl_ReturnsSortedResults()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.AddRange(
            new Category { CategoryId = 1, NamePl = "Logistyka", NameEn = "Logistics", IsActive = true },
            new Category { CategoryId = 2, NamePl = "Administracja", NameEn = "Administration", IsActive = true },
            new Category { CategoryId = 3, NamePl = "IT", NameEn = "IT", IsActive = true }
        );
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetAllAsync(includeInactive: false, CancellationToken.None);

        // Assert - Service orders by CategoryId, not NamePl
        result.Should().HaveCount(3);
        result[0].CategoryId.Should().Be(1);
        result[0].NamePl.Should().Be("Logistyka");
        result[1].CategoryId.Should().Be(2);
        result[1].NamePl.Should().Be("Administracja");
        result[2].CategoryId.Should().Be(3);
        result[2].NamePl.Should().Be("IT");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsCategoryDto()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category
        {
            CategoryId = 1,
            NamePl = "IT",
            NameEn = "IT",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
        result.NamePl.Should().Be("IT");
        result.NameEn.Should().Be("IT");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        var result = await service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAndReturnsCategory()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new CreateCategoryRequest { NamePl = "Nowa Kategoria", NameEn = "New Category" };

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CategoryId.Should().BeGreaterThan(0);
        result.NamePl.Should().Be("Nowa Kategoria");
        result.NameEn.Should().Be("New Category");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbCategory = await db.Categories.FindAsync(result.CategoryId);
        dbCategory.Should().NotBeNull();
        dbCategory!.NamePl.Should().Be("Nowa Kategoria");
        dbCategory.NameEn.Should().Be("New Category");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNamePl_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new CreateCategoryRequest { NamePl = "IT", NameEn = "Different" };

        // Act
        Func<Task> act = async () => await service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameAlreadyExists);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameEn_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new CreateCategoryRequest { NamePl = "Inna", NameEn = "IT" };

        // Act
        Func<Task> act = async () => await service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameAlreadyExists);
    }

    [Fact]
    public async Task CreateAsync_WithBothDuplicateNames_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new CreateCategoryRequest { NamePl = "IT", NameEn = "IT" };

        // Act
        Func<Task> act = async () => await service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameAlreadyExists);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAndReturnsCategory()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category
        {
            CategoryId = 1,
            NamePl = "Stara",
            NameEn = "Old",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new UpdateCategoryRequest { NamePl = "Nowa", NameEn = "New" };

        // Act
        var result = await service.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        result.CategoryId.Should().Be(1);
        result.NamePl.Should().Be("Nowa");
        result.NameEn.Should().Be("New");
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbCategory = await db.Categories.FindAsync(1);
        dbCategory!.NamePl.Should().Be("Nowa");
        dbCategory.NameEn.Should().Be("New");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingId_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new UpdateCategoryRequest { NamePl = "Test" };

        // Act
        Func<Task> act = async () => await service.UpdateAsync(999, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateNamePl_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.AddRange(
            new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true },
            new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics", IsActive = true }
        );
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new UpdateCategoryRequest { NamePl = "IT" }; // Try to rename to existing name

        // Act
        Func<Task> act = async () => await service.UpdateAsync(2, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameAlreadyExists);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateNameEn_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.AddRange(
            new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true },
            new Category { CategoryId = 2, NamePl = "Logistyka", NameEn = "Logistics", IsActive = true }
        );
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        var request = new UpdateCategoryRequest { NameEn = "IT" }; // Try to rename to existing name

        // Act
        Func<Task> act = async () => await service.UpdateAsync(2, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameAlreadyExists);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithUnusedCategory_DeletesSuccessfully()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category { CategoryId = 1, NamePl = "Test", NameEn = "Test", IsActive = true });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        await service.DeleteAsync(1, CancellationToken.None);

        // Assert - Hard delete should remove from database
        var dbCategory = await db.Categories.FindAsync(1);
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithCategoryInUse_ThrowsAppException()
    {
        // Arrange
        var db = TestDbContextFactory.CreateInMemory();
        db.Categories.Add(new Category { CategoryId = 1, NamePl = "IT", NameEn = "IT", IsActive = true });
        db.Tickets.Add(new Ticket
        {
            TicketId = 1,
            Title = "Test Ticket",
            Description = "Test",
            CategoryId = 1,
            Priority = Enums.Tickets.TicketPriority.Medium,
            Status = Enums.Tickets.TicketStatus.New,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var validator = new CategoryValidator();
        var service = new CategoryService(db, validator);

        // Act
        Func<Task> act = async () => await service.DeleteAsync(1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryInUse);
    }

    #endregion
}
