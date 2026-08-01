using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Helpers;

/// <summary>
/// Provides a PostgreSQL container for integration tests.
/// </summary>
public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresTestContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketing_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
