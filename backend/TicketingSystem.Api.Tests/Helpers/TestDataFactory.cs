using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Bogus;
using Microsoft.AspNetCore.Http;
using Moq;

using TicketingSystem.Api.DTOs.Comments;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Services;

namespace TicketingSystem.Api.Tests.Helpers;

public static class TestDataFactory
{
    private static readonly Faker Faker = new();

    private static readonly Faker<CreateTicketRequest> CreateTicketRequestFaker = new Faker<CreateTicketRequest>()
        .RuleFor(t => t.Title, f => f.Lorem.Sentence(3).TrimEnd('.'))
        .RuleFor(t => t.Description, f => string.Join(' ', f.Lorem.Sentences(3)))
        .RuleFor(t => t.CategoryId, f => f.Random.Int(1, 3))
        .RuleFor(t => t.Priority, f => f.PickRandom<TicketPriority>())
        .RuleFor(t => t.Files, _ => new List<IFormFile>());

    public static CreateTicketRequest CreateValidTicketRequest(Action<CreateTicketRequest>? configure = null)
    {
        var request = CreateTicketRequestFaker.Generate();
        configure?.Invoke(request);
        return request;
    }

    public static UpdateTicketRequest CreateUpdateRequest(Action<UpdateTicketRequest>? configure = null)
    {
        var request = new UpdateTicketRequest
        {
            Title = Faker.Lorem.Sentence(3).TrimEnd('.'),
            Description = string.Join(' ', Faker.Lorem.Sentences(2)),
            CategoryId = Faker.Random.Int(1, 3),
            Priority = Faker.PickRandom<TicketPriority>(),
            Status = Faker.PickRandom(TicketStatus.Open, TicketStatus.InProcess)
        };
        configure?.Invoke(request);
        return request;
    }

    public static AddCommentRequest CreateCommentRequest(Action<AddCommentRequest>? configure = null)
    {
        var request = new AddCommentRequest { Content = string.Join(' ', Faker.Lorem.Sentences(2)) };
        configure?.Invoke(request);
        return request;
    }

    public static IFormFile CreateFormFile(string fileName, string content, string contentType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return formFile;
    }

    public static IEnumerable<IFormFile> CreateFormFiles(int count, string contentType = "text/plain")
    {
        for (var i = 0; i < count; i++)
        {
            yield return CreateFormFile($"file-{i + 1}.txt", Faker.Lorem.Paragraph(), contentType);
        }
    }

    /// <summary>
    /// Creates a mock ICurrentUserService for testing.
    /// </summary>
    /// <param name="userId">User ID to return (default: 1)</param>
    /// <returns>Mock ICurrentUserService</returns>
    public static ICurrentUserService CreateMockCurrentUserService(int userId = 1)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.GetUserId()).Returns(userId);
        return mock.Object;
    }
}
