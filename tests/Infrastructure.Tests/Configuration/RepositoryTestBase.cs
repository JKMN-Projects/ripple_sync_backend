using Npgsql;
using Respawn;
using System.Data;

namespace RippleSync.Infrastructure.Tests.Configuration;

internal abstract class RepositoryTestBase : IAsyncLifetime, IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;
    private Respawner _respawner = default!;
    protected NpgsqlConnection DbConnection { get; private set; } = default!;
    protected RepositoryTestBase(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
        DbConnection = new NpgsqlConnection(_fixture.ConnectionString);
    }

    protected async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        if (DbConnection.State != ConnectionState.Open)
        {
            DbConnection.Open();
        }

        _respawner = await Respawner.CreateAsync(DbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = [
                //new Table()
            ]
        });
    }
    public Task DisposeAsync() => Task.CompletedTask;
}