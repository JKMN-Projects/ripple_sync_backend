using RippleSync.Application.Common;
using System.Data;

namespace RippleSync.Tests.Shared.TestDoubles;

public static class UnitOfWorkDoubles
{
    public class Dummy : IUnitOfWork
    {
        public virtual IDbConnection Connection => throw new NotImplementedException();
        public virtual IDbTransaction? Transaction => throw new NotImplementedException();

        public virtual void BeginTransaction() => throw new NotImplementedException();
        public virtual void Cancel() => throw new NotImplementedException();
        public virtual void Dispose() => throw new NotImplementedException();
        public virtual void Save() => throw new NotImplementedException();
    }

    public sealed class Composite : IUnitOfWork
    {
        private readonly IUnitOfWork[] _unitsOfWork;
        public Composite(params IUnitOfWork[] unitsOfWork)
        {
            _unitsOfWork = unitsOfWork;
        }

        public IDbConnection Connection
        {
            get
            {
                foreach (var uow in _unitsOfWork)
                {
                    if (uow.Connection != null)
                    {
                        return uow.Connection;
                    }
                }
                throw new InvalidOperationException("No underlying unit of work has a connection.");
            }
        }


        public IDbTransaction? Transaction
        {
            get
            {
                foreach (var uow in _unitsOfWork)
                {
                    if (uow.Transaction != null)
                    {
                        return uow.Transaction;
                    }
                }
                return null;
            }
        }

        public void BeginTransaction()
        {
            foreach (var uow in _unitsOfWork)
            {
                try
                {
                    uow.BeginTransaction();
                }
                catch (NotImplementedException)
                {
                    // Handle exceptions as needed
                }
            }
        }
        public void Cancel()
        {
            foreach (var uow in _unitsOfWork)
            {
                try
                {
                    uow.Cancel();
                }
                catch (NotImplementedException)
                {
                    // Handle exceptions as needed
                }
            }
        }
        public void Dispose()
        {
            foreach (var uow in _unitsOfWork)
            {
                try
                {
                    uow.Dispose();
                }
                catch (NotImplementedException)
                {
                    // Handle exceptions as needed
                }
            }
        }
        public void Save()
        {
            foreach (var uow in _unitsOfWork)
            {
                try
                {
                    uow.Save();
                }
                catch (NotImplementedException)
                {
                    // Handle exceptions as needed
                }
            }
        }
    }

    public static class Spies
    {
        public class SaveSpy : Dummy
        {
            private readonly IUnitOfWork _spied;

            public int InvocationCount { get; private set; } = 0;
            public SaveSpy(IUnitOfWork spied)
            {
                _spied = spied;
            }

            public override void Save()
            {
                InvocationCount++;
                _spied.Save();
            }
        }
    }

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
