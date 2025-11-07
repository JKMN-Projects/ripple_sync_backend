namespace RippleSync.Infrastructure.JukmanORM.Exceptions;
public class RepositoryException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
}
