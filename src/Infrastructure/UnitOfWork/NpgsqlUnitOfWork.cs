using Npgsql;
using System.Data;

namespace RippleSync.Infrastructure.UnitOfWork;
internal class NpgsqlUnitOfWork : IDisposable, IUnitOfWork
{
    private readonly NpgsqlDataSource _dataSource;
    private NpgsqlConnection? _connection;
    private bool _disposed;

    private bool _transactionManaged;

    public NpgsqlUnitOfWork(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.Build();
    }

    public NpgsqlConnection Connection
    {
        get
        {
            if (Transaction == null || _connection == null)
            {
                _connection = _dataSource.OpenConnection();
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            return _connection;
        }
    }

    public NpgsqlTransaction? Transaction { get; private set; }

    public async Task BeginTransactionAsync()
    {
        if (Transaction != null)
            throw new InvalidOperationException("Transaction already active");

        Transaction = await Connection.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (Transaction == null)
            throw new InvalidOperationException("No active transaction");

        await Transaction.CommitAsync();
        _transactionManaged = true;
        await Transaction.DisposeAsync();

        Transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (Transaction == null)
            throw new InvalidOperationException("No active transaction");

        await Transaction.RollbackAsync();
        _transactionManaged = true;
        await Transaction.DisposeAsync();

        Transaction = null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (Transaction != null && !_transactionManaged)
        {
            Transaction.Rollback();
        }

        Transaction?.Dispose();
        _connection?.Dispose();
        _dataSource?.Dispose();

        _disposed = true;
    }
}
