namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class RepositoryException(string message, QueryException? qException = null, Exception? otherException = null) : Exception
{
    public string BackendMessage { get; private set; } = message;
    public QueryException? QException { get; private set; } = qException;
    public Exception? OtherException { get; private set; } = otherException;
}
