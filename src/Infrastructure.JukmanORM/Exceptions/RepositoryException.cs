namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class RepositoryException : Exception
{
    public string BackendMessage;
    public QueryException? QException;
    public Exception? OtherException;

    public RepositoryException(string message, QueryException? qException = null, Exception? otherException = null)
    {
        BackendMessage = message;
        QException = qException;
        OtherException = otherException;
    }
}
