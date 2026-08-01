using System;
using System.IO;
using System.Linq;

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

using TicketingSystem.Api.Common;
using TicketingSystem.Api.Utils;
using TicketingSystem.Api.Validators;
using TicketingSystem.Api.Tests.Helpers;

namespace TicketingSystem.Api.Tests.Validators;

public class AttachmentValidatorTests
{
    private static AttachmentValidator CreateValidator(FileUploadOptions? options = null)
    {
        options ??= new FileUploadOptions
        {
            MaxFiles = 3,
            MaxFileSizeBytes = 5 * 1024 * 1024,
            MaxTotalSizeBytes = 10 * 1024 * 1024,
            AllowedContentTypes = new[] { "text/plain", "application/pdf" }
        };
        return new AttachmentValidator(Options.Create(options));
    }

    [Fact]
    public void ValidateFiles_WithValidFiles_DoesNotThrow()
    {
        var validator = CreateValidator();
        var files = TestDataFactory.CreateFormFiles(2).ToArray();

        Action act = () => validator.ValidateFiles(files.Cast<IFormFile>().ToArray());

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFiles_WithNullFiles_DoesNotThrow()
    {
        var validator = CreateValidator();

        Action act = () => validator.ValidateFiles(Array.Empty<IFormFile>());

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFiles_WithTooManyFiles_ThrowsAppValidationException()
    {
        var validator = CreateValidator(new FileUploadOptions { MaxFiles = 1, MaxFileSizeBytes = 1024, MaxTotalSizeBytes = 2048, AllowedContentTypes = new[] { "text/plain" } });
        var files = TestDataFactory.CreateFormFiles(2).ToArray();

        Action act = () => validator.ValidateFiles(files.Cast<IFormFile>().ToArray());

		var ex = act.Should().Throw<AppValidationException>().Which;
		ex.Errors.Should().ContainKey("Files");
		ex.Errors["Files"].Should().Contain(ErrorCodes.TooManyFiles);
    }

    [Fact]
    public void ValidateFiles_WithTooLargeFile_ThrowsAppValidationException()
    {
        var options = new FileUploadOptions
        {
            MaxFiles = 3,
            MaxFileSizeBytes = 10,
            MaxTotalSizeBytes = 100,
            AllowedContentTypes = new[] { "text/plain" }
        };
        var validator = CreateValidator(options);

        var file = TestDataFactory.CreateFormFile("big.txt", new string('a', 50));

        Action act = () => validator.ValidateFiles(new[] { file });

		var ex = act.Should().Throw<AppValidationException>().Which;
		ex.Errors["Files"].Should().Contain(ErrorCodes.FileTooLarge);
    }

    [Fact]
    public void ValidateFiles_WithInvalidContentType_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var file = TestDataFactory.CreateFormFile("file.bin", "data", "application/octet-stream");

        Action act = () => validator.ValidateFiles(new[] { file });

		var ex = act.Should().Throw<AppValidationException>().Which;
		ex.Errors["Files"].Should().Contain(ErrorCodes.FileTypeNotAllowed);
    }

    [Fact]
    public void ValidateFiles_WithEmptyFile_ThrowsAppValidationException()
    {
        var validator = CreateValidator();
        var emptyStream = new MemoryStream();
        var emptyFile = new FormFile(emptyStream, 0, 0, "file", "empty.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        Action act = () => validator.ValidateFiles(new[] { emptyFile });

		var ex = act.Should().Throw<AppValidationException>().Which;
		ex.Errors["Files"].Should().Contain(ErrorCodes.EmptyFile);
    }

    [Fact]
        public void ValidateFiles_WithMixedValidAndInvalid_ReturnsAllErrorsAtOnce()
    {
        var validator = CreateValidator();
        var valid = TestDataFactory.CreateFormFile("valid.txt", "data", "text/plain");
        var invalid = TestDataFactory.CreateFormFile("invalid.bin", "data", "application/octet-stream");

        Action act = () => validator.ValidateFiles(new[] { invalid, valid });

		var ex = act.Should().Throw<AppValidationException>().Which;
		ex.Errors["Files"].Should().Contain(ErrorCodes.FileTypeNotAllowed);
    }
}
