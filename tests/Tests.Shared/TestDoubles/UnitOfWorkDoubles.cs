using RippleSync.Application.Common;
using System.Data;

namespace RippleSync.Tests.Shared.TestDoubles;
public static class UnitOfWorkDoubles
{
    public static class Fakes
    {
        public class DoesNothing : IUnitOfWork
        {
            public IDbConnection Connection => default!;

            public IDbTransaction? Transaction => null;

            public void BeginTransaction() { }
            public void Save() { }
            public void Dispose() { }
            public void Cancel() { }
        }
    }
}
