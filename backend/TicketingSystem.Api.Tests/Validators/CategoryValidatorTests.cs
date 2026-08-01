using FluentAssertions;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Validators.Categories;
using Xunit;

namespace TicketingSystem.Api.Tests.Validators;

public class CategoryValidatorTests
{
    private readonly ICategoryValidator _validator;

    public CategoryValidatorTests()
    {
        _validator = new CategoryValidator();
    }

    #region CreateCategoryRequest Tests

    [Fact]
    public void ValidateCreate_WithValidRequest_ReturnsNormalizedRequest()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = "  Finanse  ",
            NameEn = "  Finance  "
        };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.Should().NotBeNull();
        result.NamePl.Should().Be("Finanse");
        result.NameEn.Should().Be("Finance");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WithEmptyNamePl_ThrowsAppException(string? namePl)
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = namePl,
            NameEn = "Finance"
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired)
            .WithMessage("Polish name is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateCreate_WithEmptyNameEn_ThrowsAppException(string? nameEn)
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = "Finanse",
            NameEn = nameEn
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired)
            .WithMessage("English name is required");
    }

    [Theory]
    [InlineData("A", "Too short Polish name")]
    [InlineData("AB", "Valid Polish name")]
    public void ValidateCreate_WithNameTooShort_ThrowsAppException(string namePl, string scenario)
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = namePl,
            NameEn = "Finance"
        };

        // Act & Assert
        if (scenario.Contains("Too short"))
        {
            Action act = () => _validator.ValidateAndNormalize(request);
            act.Should().Throw<AppException>()
                .Where(ex => ex.Code == ErrorCodes.CategoryNameTooShort)
                .WithMessage("Polish name is too short");
        }
        else
        {
            // Valid length - should not throw
            var result = _validator.ValidateAndNormalize(request);
            result.Should().NotBeNull();
        }
    }

    [Fact]
    public void ValidateCreate_WithNameTooLong_ThrowsAppException()
    {
        // Arrange - Create name with 101 characters
        var longName = new string('A', 101);
        var request = new CreateCategoryRequest
        {
            NamePl = longName,
            NameEn = "Finance"
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooLong)
            .WithMessage("Polish name is too long");
    }

    #endregion

    #region UpdateCategoryRequest Tests

    [Fact]
    public void ValidateUpdate_WithValidRequest_ReturnsNormalizedRequest()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            NamePl = "  Finanse  ",
            NameEn = "  Finance  ",
            IsActive = true
        };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.Should().NotBeNull();
        result.NamePl.Should().Be("Finanse");
        result.NameEn.Should().Be("Finance");
        result.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WithEmptyNamePl_ThrowsAppException(string namePl)
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            NamePl = namePl,
            NameEn = null,
            IsActive = null
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired)
            .WithMessage("Polish name cannot be empty");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUpdate_WithEmptyNameEn_ThrowsAppException(string nameEn)
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            NamePl = null,
            NameEn = nameEn,
            IsActive = null
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired)
            .WithMessage("English name cannot be empty");
    }

    [Fact]
    public void ValidateUpdate_WithAllNullFields_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            NamePl = null,
            NameEn = null,
            IsActive = null
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ValidationFailed)
            .WithMessage("No fields to update");
    }

    #endregion
}
