namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class QueryException(string message, string failedQuery = "", object? param = null, Exception? innerException = null) : Exception
{
    public string QMessage { get; private set; } = message;
    public string FailedQuery { get; private set; } = failedQuery;
    public object? Param { get; private set; } = param;

    public Exception? QInnerException { get; private set; } = innerException;
}
