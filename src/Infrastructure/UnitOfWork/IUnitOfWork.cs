using Npgsql;

namespace RippleSync.Infrastructure.UnitOfWork;
internal interface IUnitOfWork
{
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction? Transaction { get; }

    Task BeginTransactionAsync();
    Task CommitAsync();
    void Dispose();
    Task RollbackAsync();
}