namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class QueryException : Exception
{
    public string QMessage;
    public string FailedQuery;
    public object? Param;

    public Exception? QInnerException;

    public QueryException(string message, string failedQuery = "", object? param = null, Exception? innerException = null)
    {
        QMessage = message;
        FailedQuery = failedQuery;
        Param = param;
        QInnerException = innerException;
    }
}
