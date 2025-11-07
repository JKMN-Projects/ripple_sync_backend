using Npgsql;
using Respawn;
using Respawn.Graph;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Domain.Platforms;
using RippleSync.Infrastructure.UnitOfWork;
using System.Data;

namespace RippleSync.Infrastructure.Tests.Configuration;

public abstract class RepositoryTestBase : IAsyncLifetime, IClassFixture<PostgresDatabaseFixture>
{
    private readonly PostgresDatabaseFixture _fixture;
    private Respawner _respawner = default!;

    protected NpgsqlConnection DbConnection { get; private set; } = default!;
    protected IUnitOfWork UnitOfWork { get; private set; } = default!;

    protected RepositoryTestBase(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
        DbConnection = new NpgsqlConnection(_fixture.ConnectionString);
        UnitOfWork = new NpgsqlUnitOfWork(_fixture.ConnectionString);
    }

    protected async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(DbConnection);
    }

    public virtual async Task InitializeAsync()
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
                new Table("token_type"),
                new Table("post_status"),
                //new Table("platform"),
            ]
        });
        await new DataSeeder.Platforms(DbConnection).SeedPlatformsAsync(Enum.GetValues<Platform>());
    }
    public virtual Task DisposeAsync() => Task.CompletedTask;
}