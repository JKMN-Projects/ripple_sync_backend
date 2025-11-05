
using DbMigrator;
using Testcontainers.PostgreSql;

namespace RippleSync.Infrastructure.Tests.Configuration;

internal class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("ripple_sync_test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => postgreSqlContainer.GetConnectionString() + ";Include Error Detail=True";

    public async Task InitializeAsync()
    {
        await postgreSqlContainer.StartAsync();

        DatabaseMigrator.MigrateDatabase(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await postgreSqlContainer.StopAsync();
        await postgreSqlContainer.DisposeAsync();
    }
}
