using Npgsql;
using RippleSync.Application.Common.UnitOfWork;
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

    public IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = _dataSource.OpenConnection();
            }
            else if (_connection.State is ConnectionState.Closed or ConnectionState.Broken)
            {
                // Connection is dead but transaction might still reference it
                if (Transaction != null)
                {
                    Transaction.Dispose();
                    Transaction = null;
                }

                _connection.Dispose();
                _connection = _dataSource.OpenConnection();
            }

            return _connection;
        }
    }

    public IDbTransaction? Transaction { get; private set; }

    public void BeginTransaction() 
        => Transaction = Connection.BeginTransaction();

    public void Save()
    {
        if (Transaction == null)
            throw new InvalidOperationException("No active transaction");

        Transaction.Commit();
        _transactionManaged = true;
        Transaction.Dispose();

        Transaction = null;
    }

    public void Cancel()
    {
        if (Transaction == null)
            throw new InvalidOperationException("No active transaction");

        Transaction.Rollback();
        _transactionManaged = true;
        Transaction.Dispose();

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
