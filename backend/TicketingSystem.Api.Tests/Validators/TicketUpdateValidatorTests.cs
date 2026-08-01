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

public class TicketUpdateValidatorTests
{
    private static TicketUpdateValidator CreateValidator() => new(Options.Create(new TicketOptions()));

    [Fact]
    public void ValidateAndNormalize_WithValidUpdate_ReturnsNormalizedValues()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            Title = "  Updated Title  ",
            Description = new string('y', 40),
            Priority = TicketPriority.High
        };

        var normalized = validator.ValidateAndNormalize(request);

        normalized.Title.Should().Be("Updated Title");
        normalized.Description.Should().HaveLength(40);
        normalized.HasAnyChange.Should().BeTrue();
    }

    [Fact]
    public void ValidateAndNormalize_WithNoChanges_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest();

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppValidationException>()
            .Where(ex => ex.Code == ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void ValidateAndNormalize_WithTooShortTitle_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            Title = "abc"
        };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppValidationException>()
            .Which.Errors.Should().ContainKey("Title")
            .WhoseValue.Should().Contain(ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithEmptyTitle_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            Title = string.Empty
        };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppValidationException>()
            .Which.Errors.Should().ContainKey("Title")
            .WhoseValue.Should().Contain(ErrorCodes.TicketTitleTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithMultipleErrors_ReturnsAllErrors()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            Title = "ab",           // Too short (< 5)
            Description = "xyz"     // Too short (< 20)
        };

        Action act = () => validator.ValidateAndNormalize(request);

        var exception = act.Should().Throw<AppValidationException>().Which;
        exception.Errors.Should().HaveCount(2);
        exception.Errors["Title"].Should().Contain(ErrorCodes.TicketTitleTooShort);
        exception.Errors["Description"].Should().Contain(ErrorCodes.TicketDescriptionTooShort);
    }

    [Fact]
    public void ValidateAndNormalize_WithConflictingAssignment_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            AssignedToUserId = 5,
            ClearAssignment = true
        };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppValidationException>()
            .Which.Errors.Should().ContainKey("AssignedToUserId")
            .WhoseValue.Should().Contain(ErrorCodes.ValidationFailed);
    }

    [Fact]
    public void ValidateAndNormalize_WithInvalidStatus_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var request = new UpdateTicketRequest
        {
            Status = (TicketStatus)255
        };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppValidationException>()
            .Which.Errors.Should().ContainKey("Status")
            .WhoseValue.Should().Contain(ErrorCodes.TicketStatusInvalid);
    }
}
