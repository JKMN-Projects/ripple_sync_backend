using System.Data;

namespace RippleSync.Application.Common.UnitOfWork;
public interface IUnitOfWork
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }

    void BeginTransaction();
    void Save();
    void Dispose();
    void Cancel();
}