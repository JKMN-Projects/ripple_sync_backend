using Npgsql;
using RippleSync.Infrastructure.UnitOfWork;

namespace RippleSync.Infrastructure.Base;
internal class BaseRepository(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    protected NpgsqlConnection Connection => _unitOfWork.Connection;
    protected NpgsqlTransaction? Transaction => _unitOfWork.Transaction;
}
