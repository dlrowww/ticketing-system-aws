using FluentAssertions;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Categories;
using TicketingSystem.Api.Validators.Categories;
using Xunit;

namespace TicketingSystem.Api.Tests.Validators.Categories;

public class CategoryValidatorTests
{
    private readonly CategoryValidator _validator = new();

    #region CreateCategoryRequest Tests

    [Fact]
    public void ValidateAndNormalize_Create_WithValidRequest_ReturnsNormalizedRequest()
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = "  IT  ", NameEn = "  Information Technology  " };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.Should().NotBeNull();
        result.NamePl.Should().Be("IT");
        result.NameEn.Should().Be("Information Technology");
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithNullRequest_ThrowsAppException()
    {
        // Act
        Action act = () => _validator.ValidateAndNormalize((CreateCategoryRequest)null!);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithEmptyPolishName_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = "", NameEn = "IT" };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithEmptyEnglishName_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = "IT", NameEn = "" };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_Create_WithWhitespacePolishName_ThrowsAppException(string namePl)
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = namePl, NameEn = "IT" };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithPolishNameTooShort_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = "I", NameEn = "IT" }; // 1 char (min is 2)

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithPolishNameTooLong_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = new string('A', 101), // 101 chars (max is 100)
            NameEn = "IT"
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooLong);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithEnglishNameTooShort_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest { NamePl = "IT", NameEn = "A" }; // 1 char (min is 2)

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithEnglishNameTooLong_ThrowsAppException()
    {
        // Arrange
        var request = new CreateCategoryRequest
        {
            NamePl = "IT",
            NameEn = new string('A', 101) // 101 chars (max is 100)
        };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooLong);
    }

    #endregion

    #region UpdateCategoryRequest Tests

    [Fact]
    public void ValidateAndNormalize_Update_WithValidPartialRequest_ReturnsNormalizedRequest()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NamePl = "  Nowa Nazwa  " };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.Should().NotBeNull();
        result.NamePl.Should().Be("Nowa Nazwa");
        result.NameEn.Should().BeNull();
        result.IsActive.Should().BeNull();
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithNullRequest_ThrowsAppException()
    {
        // Act
        Action act = () => _validator.ValidateAndNormalize((UpdateCategoryRequest)null!);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithAllNullFields_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NamePl = null, NameEn = null, IsActive = null };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithEmptyPolishName_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NamePl = "" };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameRequired);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithPolishNameTooShort_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NamePl = "A" }; // 1 char

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithPolishNameTooLong_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NamePl = new string('A', 101) };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooLong);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithEnglishNameTooShort_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NameEn = "A" }; // 1 char

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithEnglishNameTooLong_ThrowsAppException()
    {
        // Arrange
        var request = new UpdateCategoryRequest { NameEn = new string('A', 101) };

        // Act
        Action act = () => _validator.ValidateAndNormalize(request);

        // Assert
        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CategoryNameTooLong);
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithOnlyIsActiveSet_IsValid()
    {
        // Arrange
        var request = new UpdateCategoryRequest { IsActive = false };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        result.NamePl.Should().BeNull();
        result.NameEn.Should().BeNull();
    }

    [Fact]
    public void ValidateAndNormalize_Update_WithAllFieldsSet_ReturnsNormalizedRequest()
    {
        // Arrange
        var request = new UpdateCategoryRequest
        {
            NamePl = "  Nowa  ",
            NameEn = "  New  ",
            IsActive = false
        };

        // Act
        var result = _validator.ValidateAndNormalize(request);

        // Assert
        result.NamePl.Should().Be("Nowa");
        result.NameEn.Should().Be("New");
        result.IsActive.Should().BeFalse();
    }

    #endregion
}
