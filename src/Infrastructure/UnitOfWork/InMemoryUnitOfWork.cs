using RippleSync.Application.Common.UnitOfWork;
using System.Data;

namespace RippleSync.Infrastructure.UnitOfWork;

internal class InMemoryUnitOfWork : IUnitOfWork
{
    public IDbConnection Connection => default!;

    public IDbTransaction? Transaction => null;

    public void BeginTransaction() { }
    public void Cancel() { }
    public void Dispose() { }
    public void Save() { }
}
