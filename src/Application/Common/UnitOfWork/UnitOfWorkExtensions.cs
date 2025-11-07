namespace RippleSync.Application.Common.UnitOfWork;

public static class UnitOfWorkExtensions
{
    public static async Task ExecuteInTransactionAsync(this IUnitOfWork unitOfWork, Func<Task> action)
    {
        unitOfWork.BeginTransaction();
        try
        {
            await action();
            unitOfWork.Save();
        }
        catch
        {
            unitOfWork.Cancel();
            throw;
        }
    }
}