using System.Threading.Tasks;

using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace TicketingSystem.Api.Tests.Helpers;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketing_unit_tests")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to start PostgreSQL Testcontainer. Ensure Docker is running and accessible.",
                ex);
        }

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
