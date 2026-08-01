using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TicketingSystem.Api.Data;

namespace TicketingSystem.Api.IntegrationTests.Helpers;

/// <summary>
/// Custom web application factory that provisions an isolated PostgreSQL database per test run.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _baseConnectionString;
    private readonly string _databaseName;
    private readonly string _connectionString;
    private bool _databaseCreated;

    private TestWebApplicationFactory(string baseConnectionString)
    {
        _baseConnectionString = baseConnectionString ?? throw new ArgumentNullException(nameof(baseConnectionString));

        _databaseName = $"ticketing_it_{Guid.NewGuid():N}";
        _connectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = _databaseName
        }.ConnectionString;
    }

    public static async Task<TestWebApplicationFactory> CreateAsync(string baseConnectionString)
    {
        var factory = new TestWebApplicationFactory(baseConnectionString);
        await factory.InitializeAsync();
        return factory;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                // EmailOptions used by the API are TicketingSystem.Api.Infrastructure.Email.EmailOptions
                // Provide valid values so ValidateDataAnnotations + ValidateOnStart don't fail in tests.
                ["Email:SmtpHost"] = "localhost",
                ["Email:SmtpPort"] = "25",
                ["Email:FromAddress"] = "no-reply@test.local",
                ["Email:FromName"] = "Ticketing System",
                ["Email:BaseUrl"] = "http://localhost",
                ["Email:UsePickupDirectory"] = "true",
                ["Email:PickupDirectoryPath"] = Path.GetTempPath(),
                // CORS configuration for testing
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        });
    }

    private async Task InitializeAsync()
    {
        var adminConnection = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
            await cmd.ExecuteNonQueryAsync();
        }

        _databaseCreated = true;
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_databaseCreated)
        {
            await base.DisposeAsync();
            return;
        }

        var adminConnection = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = "postgres"
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();

        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}';";
            await terminate.ExecuteNonQueryAsync();
        }

        await using (var drop = connection.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\";";
            await drop.ExecuteNonQueryAsync();
        }

        await base.DisposeAsync();
    }
}
