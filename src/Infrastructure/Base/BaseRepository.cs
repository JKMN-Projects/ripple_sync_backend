using Npgsql;
using RippleSync.Application.Common.UnitOfWork;

namespace RippleSync.Infrastructure.Base;
internal class BaseRepository(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));


    protected NpgsqlConnection Connection => (NpgsqlConnection)_unitOfWork.Connection;
    protected NpgsqlTransaction? Transaction => (NpgsqlTransaction?)_unitOfWork.Transaction;
}
