using System;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Tests.Validators;

public class TicketValidatorTests
{
    private static TicketValidator CreateValidator() => new(Options.Create(new TicketOptions()));

    [Fact]
    public void ValidateAndNormalize_WithValidRequest_ReturnsNormalizedInput()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "   Network issue   ",
            Description = new string('x', 40),
            CategoryId = 1, // IT
            Priority = TicketPriority.High
        };

        var normalized = validator.ValidateAndNormalize(request);

        normalized.Title.Should().Be("Network issue");
        normalized.Description.Should().HaveLength(40);
        normalized.CategoryId.Should().Be(1);
        normalized.Priority.Should().Be(TicketPriority.High);
    }

    [Fact]
    public void ValidateAndNormalize_WithShortTitle_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "abc",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketPriority.Low
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors.Should().ContainKey("Title");
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithEmptyTitle_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = string.Empty,
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketPriority.Low
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithWhitespaceOnlyTitle_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "    ",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketPriority.Medium
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithTitleTooLong_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = new string('t', 300),
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = TicketPriority.Medium
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooLong);
    }

    [Fact]
    public void ValidateAndNormalize_WithShortDescription_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "Valid Title",
            Description = "short",
            CategoryId = 1,
            Priority = TicketPriority.Low
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Description"].Should().Contain(ErrorCodes.TicketDescriptionTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithEmptyDescription_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "Valid Title",
            Description = string.Empty,
            CategoryId = 1,
            Priority = TicketPriority.Low
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Description"].Should().Contain(ErrorCodes.TicketDescriptionTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithAnyCategoryId_PassesValidation()
    {
        // Note: CategoryId validation against DB happens in service layer, not validator
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "Valid Title",
            Description = new string('x', 40),
            CategoryId = 999, // Any non-null ID passes validator (DB check is later)
            Priority = TicketPriority.Low
        };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().NotThrow(); // Validator only checks nullability, not existence
    }

    [Fact]
    public void ValidateAndNormalize_WithMissingPriority_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "Valid Title",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = null
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Priority"].Should().Contain(ErrorCodes.TicketPriorityRequired);
    }

    [Fact]
    public void ValidateAndNormalize_WithInvalidPriority_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "Valid Title",
            Description = new string('x', 40),
            CategoryId = 1,
            Priority = (TicketPriority)250
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Priority"].Should().Contain(ErrorCodes.TicketPriorityInvalid);
    }

    [Fact]
    public void ValidateAndNormalize_WithMultipleInvalidFields_ReturnsAllErrorCodes()
    {
        var validator = CreateValidator();
        var request = new CreateTicketRequest
        {
            Title = "abc",
            Description = "short",
            CategoryId = null,
            Priority = null
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var ex = act.Should().Throw<AppValidationException>().Which;
        ex.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);
        ex.Errors["Description"].Should().Contain(ErrorCodes.TicketDescriptionTooShort);
        ex.Errors["CategoryId"].Should().Contain(ErrorCodes.TicketCategoryRequired);
        ex.Errors["Priority"].Should().Contain(ErrorCodes.TicketPriorityRequired);
    }
}
