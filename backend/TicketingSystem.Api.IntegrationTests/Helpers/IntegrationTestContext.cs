using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TicketingSystem.Api.Data;

namespace TicketingSystem.Api.IntegrationTests.Helpers;

/// <summary>
/// Helper context that provides an isolated application instance and HttpClient per integration test.
/// </summary>
public sealed class IntegrationTestContext : IAsyncDisposable
{
    private readonly TestWebApplicationFactory _factory;

    private IntegrationTestContext(TestWebApplicationFactory factory, HttpClient client)
    {
        _factory = factory;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<IntegrationTestContext> CreateAsync(string baseConnectionString)
    {
        var factory = await TestWebApplicationFactory.CreateAsync(baseConnectionString);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        return new IntegrationTestContext(factory, client);
    }

    public async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public async Task<T> WithDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
