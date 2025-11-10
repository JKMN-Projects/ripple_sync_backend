
using DbMigrator;
using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace RippleSync.Infrastructure.Tests.Configuration;

public class PostgresDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;

    public string ConnectionString => _postgreSqlContainer?.GetConnectionString() + ";Include Error Detail=True";

    public async Task InitializeAsync()
    {
        var postgresImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetCallerFileDirectory(), ".")
            .WithDockerfile("Dockerfile")
            .Build();

        await postgresImage.CreateAsync();

        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage(postgresImage)
            .WithDatabase("ripple_sync_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithEnvironment("POSTGRES_INITDB_ARGS", "-c shared_preload_libraries=pg_cron")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
            .Build();

        await _postgreSqlContainer.StartAsync();

        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS pg_cron;", connection);
        await cmd.ExecuteNonQueryAsync();

        DatabaseMigrator.MigrateDatabase(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.StopAsync();
            await _postgreSqlContainer.DisposeAsync();
        }
    }
}
