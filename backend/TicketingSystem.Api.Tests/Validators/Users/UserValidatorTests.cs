using System;
using System.Net;

using FluentAssertions;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Users;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Validators.Users;

namespace TicketingSystem.Api.Tests.Validators.Users;

public sealed class UserValidatorTests
{
    private static readonly IUserValidator Validator = new UserValidator();

    [Fact]
    public void ValidateAndNormalize_Create_WithValidRequest_NormalizesAndReturns()
    {
        var req = new CreateUserRequest
        {
            Name = "  John Doe  ",
            Email = "  JOHN.DOE@EXAMPLE.COM ",
            Password = "Password123!",
            Role = (byte)UserRole.Employee,
            CategoryId = null
        };

        var result = Validator.ValidateAndNormalize(req);

        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.Password.Should().Be("Password123!");
        result.Role.Should().Be((byte)UserRole.Employee);
        result.CategoryId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_Create_WithMissingName_Throws(string? name)
    {
        var req = new CreateUserRequest
        {
            Name = name!,
            Email = "john@example.com",
            Password = "Password123!",
            Role = (byte)UserRole.Employee
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserNameRequired && e.Status == HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_Create_WithMissingEmail_Throws(string? email)
    {
        var req = new CreateUserRequest
        {
            Name = "John",
            Email = email!,
            Password = "Password123!",
            Role = (byte)UserRole.Employee
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserEmailRequired && e.Status == HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    public void ValidateAndNormalize_Create_WithInvalidEmail_Throws(string email)
    {
        var req = new CreateUserRequest
        {
            Name = "John",
            Email = email,
            Password = "Password123!",
            Role = (byte)UserRole.Employee
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserEmailInvalid && e.Status == HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndNormalize_Create_WithMissingPassword_Throws(string? pwd)
    {
        var req = new CreateUserRequest
        {
            Name = "John",
            Email = "john@example.com",
            Password = pwd!,
            Role = (byte)UserRole.Employee
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserPasswordRequired && e.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithShortPassword_Throws()
    {
        var req = new CreateUserRequest
        {
            Name = "John",
            Email = "john@example.com",
            Password = "short",
            Role = (byte)UserRole.Employee
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserPasswordTooShort && e.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateAndNormalize_Create_WithInvalidRole_Throws()
    {
        var req = new CreateUserRequest
        {
            Name = "John",
            Email = "john@example.com",
            Password = "Password123!",
            Role = 250
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserRoleInvalid && e.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateAndNormalize_Create_SupportWithoutCategory_Throws()
    {
        var req = new CreateUserRequest
        {
            Name = "Support",
            Email = "support@example.com",
            Password = "Password123!",
            Role = (byte)UserRole.Support,
            CategoryId = null
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserCategoryRequired && e.Status == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateAndNormalize_Create_SupportWithCategory_Allows()
    {
        var req = new CreateUserRequest
        {
            Name = "Support",
            Email = "support@example.com",
            Password = "Password123!",
            Role = (byte)UserRole.Support,
            CategoryId = (byte)TicketCategory.IT
        };

        var result = Validator.ValidateAndNormalize(req);

        result.CategoryId.Should().Be((byte)TicketCategory.IT);
    }

    [Fact]
    public void ValidateAndNormalize_Update_AllNullFields_Allows()
    {
        var req = new UpdateUserRequest();

        var result = Validator.ValidateAndNormalize(req);

        result.Name.Should().BeNull();
        result.Email.Should().BeNull();
        result.Password.Should().BeNull();
        result.Role.Should().BeNull();
        result.CategoryId.Should().BeNull();
        result.IsActive.Should().BeNull();
    }

    [Fact]
    public void ValidateAndNormalize_Update_RoleToSupportWithoutCategory_Throws()
    {
        var req = new UpdateUserRequest
        {
            Role = (byte)UserRole.Support
        };

        Action act = () => Validator.ValidateAndNormalize(req);

        act.Should().Throw<AppException>()
            .Where(e => e.Code == ErrorCodes.UserCategoryRequired && e.Status == HttpStatusCode.BadRequest);
    }
}
