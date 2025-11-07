namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class QueryException(string message, string failedQuery = "", object? param = null, Exception? innerException = null) 
    : Exception(message, innerException)
{
    public string FailedQuery { get; private set; } = failedQuery;
    public object? Param { get; private set; } = param;
}
