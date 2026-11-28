using System;
using System.IO;
using System.Threading.Tasks;
using Cbo.Api;
using Cbo.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

[assembly: AssemblyFixture(typeof(Cbo.Tests.Integration.ApiFactory))]

namespace Cbo.Tests.Integration;

/// <summary>
/// Runs the real API on a throwaway Postgres database, created from the model and dropped after the test run. The
/// server comes from the <c>CBO_TEST_CONNECTION_STRING</c> environment variable, or else from the API's development
/// settings. Tests that need it are skipped when it can't be reached.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionStringVariable = "CBO_TEST_CONNECTION_STRING";

    private readonly string _connectionString;
    private string? _unavailableReason = "The test database hasn't been created yet.";

    public ApiFactory()
    {
        _connectionString = new NpgsqlConnectionStringBuilder(ServerConnectionString())
        {
            Database = $"cbo_test_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    /// <summary>Skips the calling test when the database server can't be reached.</summary>
    public void SkipUnlessAvailable() => Assert.SkipWhen(_unavailableReason is not null, _unavailableReason ?? string.Empty);

    public async ValueTask InitializeAsync()
    {
        var server = new NpgsqlConnectionStringBuilder(_connectionString) { Database = "postgres", Timeout = 3 };
        try
        {
            await using var connection = new NpgsqlConnection(server.ConnectionString);
            await connection.OpenAsync();
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            _unavailableReason = $"Postgres at {server.Host}:{server.Port} can't be reached: {exception.Message}";
            return;
        }

        using IServiceScope scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CboDbContext>().Database.EnsureCreatedAsync();
        _unavailableReason = null;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_unavailableReason is null)
        {
            using IServiceScope scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<CboDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Program.TestingEnvironmentName);
        builder.UseSetting("ConnectionStrings:CboDb", _connectionString);
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-integration-tests-signing-key");
        builder.UseSetting("Jwt:Issuer", "cbo-tests");
        builder.UseSetting("Jwt:Audience", "cbo-tests");
    }

    private static string ServerConnectionString()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        string settings = Path.Combine(RepoRoot.Path, "backend", "Cbo.Api", "appsettings.Development.json");
        return new ConfigurationBuilder()
            .AddJsonFile(settings, optional: true)
            .Build()
            .GetConnectionString("CboDb")
            ?? "Host=localhost; Port=5432; Username=postgres";
    }
}
