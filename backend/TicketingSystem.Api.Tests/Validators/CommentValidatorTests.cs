using System;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;

namespace TicketingSystem.Api.Tests.Validators;

public class CommentValidatorTests
{
        private static CommentValidator CreateValidator() => new(Options.Create(new CommentOptions()));

    [Fact]
    public void ValidateAndNormalize_WithValidContent_ReturnsTrimmedContent()
    {
        var validator = CreateValidator();
        var request = new AddCommentRequest { Content = "  hello world  " };

        var normalized = validator.ValidateAndNormalize(request);

        normalized.Should().Be("hello world");
    }

    [Fact]
    public void ValidateAndNormalize_WithEmptyContent_ThrowsAppException()
    {
        var validator = CreateValidator();
        var request = new AddCommentRequest { Content = "   " };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CommentEmpty);
    }

    [Fact]
    public void ValidateAndNormalize_WithTooLongContent_ThrowsAppException()
    {
        var options = new CommentOptions { MaxLength = 10, MinLength = 1 };
        var validator = new CommentValidator(Options.Create(options));
        var request = new AddCommentRequest { Content = new string('x', 20) };

        Action act = () => validator.ValidateAndNormalize(request);

        act.Should().Throw<AppException>()
            .Where(ex => ex.Code == ErrorCodes.CommentTooLong);
    }
}
