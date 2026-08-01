using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TicketingSystem.Api.DTOs.Lookups;
using TicketingSystem.Api.Enums.Identity;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.IntegrationTests.Helpers;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.CollectionName)]
public sealed class LookupsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly PostgresTestContainer _postgres;

    public LookupsControllerTests(PostgresTestContainer postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task GetLookups_ReturnsAllEnums()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var response = await ctx.Client.GetAsync("/api/lookups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().NotBeNullOrEmpty();

        var payload = await response.Content.ReadFromJsonAsync<LookupsResponse>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.TicketStatus.Should().HaveCount(Enum.GetValues<TicketStatus>().Length);
        payload.Priority.Should().HaveCount(Enum.GetValues<TicketPriority>().Length);
        // Category is now database-driven, not enum-driven (no static count assertion)
        payload.Category.Should().NotBeEmpty();
        payload.UserRole.Should().HaveCount(Enum.GetValues<UserRole>().Length);
        payload.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLookups_ReturnsCorrectEnumValues()
    {
        await using var ctx = await IntegrationTestContext.CreateAsync(_postgres.ConnectionString);

        var response = await ctx.Client.GetAsync("/api/lookups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LookupsResponse>(JsonOptions);
        payload.Should().NotBeNull();

        foreach (var item in payload!.TicketStatus)
        {
            Enum.GetValues<TicketStatus>()
                .Should().Contain(e => (int)e == item.Id);
            item.Name.Should().StartWith("ticket_status_");
        }

        foreach (var item in payload.Priority)
        {
            Enum.GetValues<TicketPriority>()
                .Should().Contain(e => (int)e == item.Id);
            item.Name.Should().StartWith("priority_");
        }

        // Category is now database-driven, not enum-driven (skip enum validation)
        payload.Category.Should().NotBeEmpty();
        foreach (var item in payload.Category)
        {
            item.Id.Should().BeGreaterThan(0);
            item.Name.Should().StartWith("category_");
        }

        foreach (var item in payload.UserRole)
        {
            Enum.GetValues<UserRole>()
                .Should().Contain(e => (int)e == item.Id);
            item.Name.Should().StartWith("role_");
        }
    }
}
