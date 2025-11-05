using Npgsql;
using RippleSync.Application.Common;
using System.Data;

namespace RippleSync.Infrastructure.UnitOfWork;
internal class NpgsqlUnitOfWork : IDisposable, IUnitOfWork
{
    private readonly NpgsqlDataSource _dataSource;
    private NpgsqlConnection? _connection;
    private bool _disposed;

    private bool _transactionManaged;

    private bool _transactionFromBiggerScope;

    public NpgsqlUnitOfWork(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.Build();
    }

    public IDbConnection Connection
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

    public IDbTransaction? Transaction { get; private set; }

    public void BeginTransaction()
    {
        if (Transaction != null)
        {
            _transactionFromBiggerScope = true;
        }
        else
        {
            Transaction = Connection.BeginTransaction();
        }
    }

    public void Save()
    {
        if (Transaction == null)
            throw new InvalidOperationException("No active transaction");

        if(!_transactionFromBiggerScope)
        {
            Transaction.Commit();
            _transactionManaged = true;
            Transaction.Dispose();

            Transaction = null;
        }
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
